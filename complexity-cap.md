# Complexity Cap: Bounding Pathological HermitCrab Parses

**Status:** Plan (not started) — sequencing and defaults decided, see §8/§10
**Author:** drafted 2026-07-02
**Related:** PR #446 (hc-rustify performance work), FieldWorks out-of-process HC worker (FW PR #983)

**Decided (2026-07-02):**
- Implement on top of `hc-rustify`, not master (§8).
- Budget breach is **soft-stop** (partial results + status), never an exception (§4.4, §10.1).
- Ship a **generous default** `MaxParseSteps`/`ParseTimeout` so naive consumers are
  protected out of the box, not pure opt-in (§4.1, §10.2).
- Use the real `samples/data/{indonesian,sena}-hc.xml` grammars + wordlists as the
  calibration and regression corpus, not synthetic-only fixtures (§7, §9 Phase 0).

## 1. Problem

PR #446 made the core HermitCrab engine much faster, but grammar-induced blowups remain:
certain grammar constructs — typically unbounded/multiple-application rules with no overt
exponent, unconstrained deletion rules, and unconstrained compounding — cause the analysis
phase to generate candidates combinatorially. A single word can take minutes to hours. No
engine speedup fixes an exponential; the grammar must be constrained. Until grammars are
fixed, we need:

1. **Bounded runtime** — a single pathological word must never hang a parse (or a
   FieldWorks "Parse All Words" batch).
2. **Actionable diagnostics** — when the engine gives up, it should say *which rule(s)*
   caused the blowup, with evidence.
3. **A "don't do this" guide** — static analysis that flags always-wrong or
   almost-always-wrong rule shapes, consumable by other tools (FLEx parser report, CLI).

## 2. Current state (inventory of existing guardrails)

All partial, none sufficient:

| Guard | Where | Limitation |
|---|---|---|
| `AffixProcessRule.MaxApplicationCount` (default 1; XML `multipleApplication` attr raises it) | `AnalysisAffixProcessRule.Apply` checks `GetUnapplicationCount(rule) >= max` | Per-rule only. Rule A → B → A → B evades it. The `multipleApplication` attribute is precisely where pathological grammars opt into unboundedness. |
| `Morpher.MaxStemCount` (default 2) | `AnalysisCompoundingRule` | Compounding only. |
| `Morpher.MaxUnapplications` (default 0 = off) | `AnalysisStratumRule.Apply` output loop | Caps the *number of analyses emitted per stratum*, not the *work* spent producing them; a cascade can burn unbounded time before emitting anything. Off by default. Confusingly named given the new caps proposed below. |
| `Morpher.DeletionReapplications` (default 0) | `AnalysisRewriteRule` | Bounds re-insertion of deleted material for *phonological rewrite rules only*. |
| Infinite-loop check | `PermutationRuleCascade.ApplyRules` (`Comparer.Equals(input, result)`) | Only catches a rule whose output equals its own input. Two-rule cycles and monotonic *growth* (hypothesizing deleted material) sail past. |
| `MergeEquivalentAnalyses` (default true) | `AnalysisStratumRule` | Dedup by shape; helps but doesn't bound. |

**There is no timeout, no cancellation, and no work budget anywhere in HC.** `ParseWord`
is synchronous; the `MaxUnapplications` doc comment itself mentions 30-minute words.

## 3. Design overview — three layers

Each layer addresses a different failure mode; do all three:

- **Layer 1 — work budget (safety net):** deterministic per-word step budget with a
  wall-clock backstop. Stops eruptions cold and produces the per-rule evidence used by
  everything else.
- **Layer 2 — structural bounds (prevention):** global per-word unapplication cap,
  analysis shape-growth cap, cascade cycle detection. Converts exponential to bounded.
- **Layer 3 — static grammar lint (guidance):** `GrammarAnalyzer` over a loaded
  `Language`, emitting structured diagnostics with stable codes; plus a written
  anti-pattern guide keyed to those codes.

### Design principles

- **Deterministic first.** A step budget fails the same way on every machine, so grammar
  authors get a reproducible signal and tests stay stable. Wall-clock timeout is only a
  backstop (machine-dependent; and 10k words × 20 s timeout each still erupts in batch).
- **Cheap happy path.** PR #446 deliberately removed `MorpherStatistics` because it was
  woven into the hot path. The budget's steady-state cost must be ~one counter increment
  per rule application; detailed per-rule counters are collected only on a **diagnostic
  re-run after a breach** (breaches are rare; re-running one word with counters on is
  cheap and keeps the hot path clean).
- **Additive API.** FieldWorks (in-process HCLoader path *and* the out-of-process worker)
  consumes `Morpher`. All new knobs are properties with backward-compatible defaults;
  existing `ParseWord`/`AnalyzeWord` signatures keep working.
- **Fail soft, report loud.** A budget breach yields the analyses found so far plus an
  explicit "gave up" status — never a silent empty result (FLEx must distinguish
  "no parse" from "gave up") and, by default, never an exception mid-batch.

## 4. Layer 1 — work budget + timeout

### 4.1 Configuration (on `Morpher`, following existing property style)

```csharp
/// Max rule applications (analysis + synthesis) per ParseWord call. 0 = unlimited.
public int MaxParseSteps { get; set; }           // ships ON with a generous default; see below
/// Wall-clock backstop per ParseWord call. Zero/infinite = disabled.
public TimeSpan ParseTimeout { get; set; }        // ships ON with a generous default; see below
```

**Default philosophy (decided):** ship generous, non-zero defaults for both, not
opt-in-only. Rationale: most consumers (machine.py users, FLEx via HCLoader, anyone
scripting `Morpher` directly) will never touch these knobs; a `0`/unlimited default means
the exact failure mode this plan exists to fix — an unbounded parse — remains the
out-of-the-box behavior. A generous cap that never fires for legitimate grammars but
reliably kills runaway ones is strictly better than silence.

Concrete numbers are calibrated in Phase 0 against the real corpus (§7), not guessed
here, but the target shape is: run every word in `indonesian-words.txt` (121 words) and
`sena-words.txt` (7,121 words) against their respective grammars on the rustify engine,
take the observed max step count / max wall-clock time across that legitimate corpus,
and set the default to a large multiple of that ceiling (e.g. 50–100×) so it is
effectively invisible for real grammars but still finite. `ParseTimeout` defaults
similarly, e.g. a flat few seconds per word — generous for interactive/FLEx single-word
parses, still bounded for "Parse All Words" batches where one stuck word must not stall
the run indefinitely.

### 4.2 Per-parse context, propagated like `CurrentTrace`

Compiled rule objects are shared across concurrent parses, so per-call state cannot live
on the rules or the `Morpher`. But every rule receives the `Word`, and `Word` already
propagates a shared reference through clones (`CurrentTrace`, Word.cs copy-ctor). Add:

```csharp
internal ParseContext ParseContext { get; set; }   // on Word; reference-shared

internal sealed class ParseContext
{
    private int _steps;                    // Interlocked — analysis fans out in parallel
    private readonly long _deadlineTicks;  // Stopwatch-based (netstandard2.0-safe)
    public bool Exhausted { get; private set; }
    public ParseExhaustionReason Reason { get; private set; }  // StepBudget | Timeout
    public bool Step()                     // returns false when budget is gone
    {
        // Interlocked.Increment; check deadline only every N (e.g. 256) steps
    }
    // Diagnostic mode (breach re-run only):
    public ConcurrentDictionary<IHCRule, int> RuleCounters { get; }
}
```

Propagation rules (mirror `CurrentTrace` exactly):
- `Word` copy-ctor copies the reference.
- Fresh `Word` constructions inside a parse (`Morpher.LexicalLookup`, `LexicalGuess`,
  `Word.CurrentNonHead` path at Word.cs:489, `GenerateWords` synthesis words) must
  re-attach the context.
- Excluded from `FreezeImpl` hashing and `ValueEquals` (like `CurrentTrace`), so dedup
  semantics are unchanged. It is mutable state on a frozen `Word` — same precedent as
  `CurrentTrace`.

### 4.3 Check sites

All in the HC assembly (the generic `SIL.Machine` cascades stay untouched — every rule
they invoke checks, which bounds cascade recursion transitively):

- `AnalysisAffixProcessRule.Apply` / `AnalysisRealizationalAffixProcessRule` /
  `AnalysisCompoundingRule` — alongside the existing `RuleSelector` /
  `MaxApplicationCount` early-outs.
- `AnalysisRewriteRule.Apply` (per iteration, not just per call — one call can loop).
- Affix template slot application.
- Synthesis counterparts (`SynthesisAffixProcessRule` etc.) — synthesis explodes too
  when analysis hands it thousands of candidates.
- `Morpher.Synthesize` / `LexicalLookup` loops — check `Exhausted` between candidates so
  the unwind is fast.

On `Step() == false`: the rule returns `Enumerable.Empty<Word>()`. **This is the only
behavior on breach — no exception path is offered for step/timeout exhaustion**, decided
because the primary target (FieldWorks "Parse All Words") is a batch over thousands of
words where one stuck word throwing would either kill the batch or force every caller to
wrap every word in try/catch. Real errors (bad grammar, bugs) still throw normally via
existing `Parallel.ForEach` exception plumbing — this only governs the "ran out of
budget" case. The parse drains quickly and naturally once `Step()` starts returning
false, since every rule-level early-out (§4.3) short-circuits immediately.

### 4.4 Result surface

```csharp
public IEnumerable<Word> ParseWord(string word, out object trace, bool guessRoot,
                                   out ParseDiagnostics diagnostics);

public sealed class ParseDiagnostics
{
    public bool BudgetExhausted { get; }
    public ParseExhaustionReason Reason { get; }        // StepBudget | Timeout | None
    public int StepsUsed { get; }
    public TimeSpan Elapsed { get; }
    /// Populated only by RerunWithDiagnostics (breach re-run).
    public IReadOnlyList<(IHCRule Rule, int Applications)> TopRules { get; }
}
```

- Existing overloads keep working (diagnostics discarded).
- `IMorphologicalAnalyzer.AnalyzeWord` is an interface shared with non-HC analyzers —
  leave it unchanged; best-effort results. Callers who need status use the new overload.
- `Morpher.RerunWithDiagnostics(string word)` (name TBD): re-parses one word with
  per-rule counters (and optionally a lower budget), returning ranked
  `(rule, applicationCount)` — "word *X* exceeded 100k steps; rule *Y* accounted for
  92% of applications." This is the empirical half of the "don't do this" guide.

### 4.5 FieldWorks / worker integration (follow-up, separate repo)

- The worker DTO (`WordAnalysisDto` / batch results in FW `Src/LexText/HCWorker`) gains a
  per-word status field (`Success | NoParse | GaveUp(reason)`), so "Parse All Words" can
  show gave-up words distinctly and offer "diagnose this word" (the re-run).
- `ParserWorker.ParseAndUpdateWordformGuarded` already guards per-word exceptions; the
  soft-stop design means it needs no change to survive breaches — only to *display* them.

## 5. Layer 2 — structural bounds

### 5.1 Global per-word unapplication cap (the "same thing, even if separated" bound)

`Word` already tracks per-rule unapplication counts (that's how `MaxApplicationCount` is
enforced). Add a running total incremented in `MorphologicalRuleUnapplied`:

```csharp
/// Max total morphological-rule unapplications per analysis candidate (≈ max affixes
/// per word). 0 = unlimited. Proposed default: 0 initially, recommend 10–16 for FLEx.
public int MaxRuleApplicationsPerWord { get; set; }
```

Checked in the same early-out cluster as `MaxApplicationCount`. This closes the
A→B→A→B loophole: no per-rule counter trips, but the total does.

Naming note: the existing `Morpher.MaxUnapplications` (caps *analyses emitted per
stratum*) is easily confused with this. Keep it, document both clearly, consider
`[Obsolete]`-forwarding it to a better name in the same release (decide in review).

### 5.2 Analysis shape-growth cap

The one truly unbounded generator is unapplication that makes the hypothesized underlying
form *longer* than the surface form (undoing deletions; empty/subtractive exponents).
`DeletionReapplications` bounds this narrowly for rewrite rules; generalize:

```csharp
/// Prune any analysis candidate whose shape exceeds the surface form by more than
/// this many segments. -1 = unlimited (default, preserves current behavior).
public int MaxAnalysisShapeGrowth { get; set; }
```

Enforced at the `AnalysisStratumRule.Apply` output loop (single choke point; candidates
pruned there never reach lexical lookup or the next stratum) and in
`AnalysisRewriteRule`'s iteration loop (so a self-feeding epenthesis-unapplication is cut
mid-rule, not after producing a huge shape). Surface length is captured on the
`ParseContext` (Layer 1's context doubles as the carrier for per-parse constants).

### 5.3 Cycle detection in the permutation cascade

`PermutationRuleCascade.ApplyRules` currently only compares a result to its immediate
input. Two options, in preference order:

1. **Depth cap (simple, sufficient):** thread a recursion-depth parameter; stop
   descending past `MaxCascadeDepth` (derivable from `MaxRuleApplicationsPerWord`, so
   possibly no new knob). Cheap, no allocation.
2. **Visited set (complete):** per-branch `HashSet<TData>` with the existing
   `FreezableEqualityComparer`. Catches length-k cycles exactly but allocates per branch.

Given Layers 1 + 5.1 already bound total work, option 1 is likely enough; implement 1,
keep 2 in reserve. These classes are in `SIL.Machine` core but consumed only by HC
(verified: `SynthesisStratumRule`, `AnalysisStratumRule`), so a constructor-injected
optional guard is safe.

### 5.4 Defaults and compatibility

All Layer-2 caps default to **off** in `SIL.Machine` (no behavior change for existing
consumers; some legitimate agglutinative grammars have long affix chains). FieldWorks
sets conservative values (proposed: `MaxRuleApplicationsPerWord` ≈ 16,
`MaxAnalysisShapeGrowth` ≈ 6, `MaxParseSteps` ≈ 250k — calibrate in Phase 0). Revisit
turning defaults on in a subsequent major version once field data exists.

## 6. Layer 3 — static grammar lint (`GrammarAnalyzer`)

### 6.1 Shape

```csharp
public static class GrammarAnalyzer
{
    public static IReadOnlyList<GrammarDiagnostic> Analyze(Language language);
}

public sealed class GrammarDiagnostic
{
    public string Code { get; }        // stable, e.g. "HC0001" — doc anchor
    public DiagnosticSeverity Severity { get; }   // Error | Warning | Info
    public IHCRule Rule { get; }       // or Morpheme/AffixTemplate — the culprit object
    public string Message { get; }
    public string Suggestion { get; }
}
```

Operates on the in-memory `Language`, so it works for **both** XML-loaded grammars and
FieldWorks' programmatically built ones (HCLoader). A thin CLI (`hc-lint grammar.xml`)
wraps `XmlLanguageLoader` + `Analyze` for use outside FLEx.

### 6.2 Check catalogue (initial)

| Code | Severity | Detects | Rationale |
|---|---|---|---|
| HC0001 | Error | Affix rule with **no overt exponent** (analysis side is a pure variable copy — LHS one `[Seg]*`-class variable, RHS adds no constant segments) **and** `MaxApplicationCount > 1` | Unapplies to every word, every time: guaranteed exponential. The headline "always wrong". |
| HC0002 | Warning | No overt exponent, `MaxApplicationCount == 1` | Still multiplies candidates once per cascade position; frequently unintended. |
| HC0003 | Warning | `multipleApplication` set high/unbounded on any rule | Flag the opt-in itself; require justification. |
| HC0004 | Warning | **Self-feeding rule**: output unifies with the rule's own required environment (epenthesis/insertion feeding itself) | Loop generator in synthesis; growth generator in analysis. |
| HC0005 | Warning | **Unconstrained deletion**: deletion rewrite rule with very permissive context | Unbounded re-insertion during analysis; interacts with `DeletionReapplications`. |
| HC0006 | Warning | Compounding rule with unconstrained POS on **both** head and non-head | Cross-product blowup; interacts with `MaxStemCount`. |
| HC0007 | Info | Optional-iterative lexical patterns (e.g. `([Seg])([Seg])`) | Spurious-ambiguity source already noted in `Morpher.LexicalGuess` comments. |
| HC0008 | Info | Cyclic feeding pair: rule A's analysis output can feed B and vice versa with net growth | Best-effort structural check; pairs only. |

What static analysis *cannot* catch — combinatorial interaction among individually
reasonable rules — is covered by Layer 1's breach re-run (empirical top-offender report).
The written guide ("Writing performant HC grammars") is organized by these codes, with a
section on interpreting the empirical report.

### 6.3 Consumers

- **FLEx**: parser report / grammar check UI lists diagnostics next to the rules
  (FieldWorks-side work, out of scope here; the API is designed for it).
- **CLI**: for machine.py users and CI-style grammar validation.
- **Tests**: our own pathological fixtures must each trip their intended code.

## 7. Testing strategy

- **Pathological fixtures**: construct minimal grammars in `MorpherTests` for each class:
  glob rule + `multipleApplication`, A↔B cycle, self-feeding epenthesis, unconstrained
  deletion, unconstrained compounding. Each must (a) trip the budget deterministically at
  a known step count, (b) be caught by its Layer-2 cap, (c) be flagged by its lint code.
- **Real-grammar fixtures (decided): use `samples/data/{indonesian,sena}-hc.xml` +
  their wordlists directly.** These are the two grammars already in the working tree
  from the rustify perf sessions — `indonesian-hc.xml` (2,563 lines) /
  `indonesian-words.txt` (121 words) and the much larger `sena-hc.xml` (33,091 lines) /
  `sena-words.txt` (7,121 words). They serve three roles: (1) the **default-calibration
  corpus** for §4.1 (measure legitimate max steps/time, set the generous default above
  it); (2) the **no-regression corpus** — with all knobs at their shipped defaults, every
  word in both wordlists must still parse to byte-identical results (rustify's own audit
  already established byte-identical output on these corpora pre-complexity-cap, so any
  divergence post-complexity-cap is a bug in this work, not noise); (3) the **overhead
  benchmark** corpus (see below). Still verify licensing/provenance before committing
  them permanently to the test tree (currently untracked).
- **Determinism**: same grammar + word ⇒ identical `StepsUsed` and identical breach
  point, single- and multi-threaded (steps counter is shared/Interlocked; the *count at
  breach* may vary ±parallelism — assert exhaustion + reason, not exact step, in parallel
  mode; assert exact step in `SINGLE_THREADED`/dop=1).
- **No-regression**: with all knobs off, full existing suite green and byte-identical
  parse results on the sample grammars.
- **Overhead benchmark**: sena + indonesian wordlists, budget on (shipped default) vs.
  budget fully disabled, on the **rustify** engine (see §8) — target < 2% throughput
  cost; if the single Interlocked increment shows up, fall back to per-thread counters
  flushed periodically.
- **Pathological additions to the real corpus**: since indonesian/sena are (presumably)
  well-behaved grammars, also hand-craft 1–2 pathological *variants* of the indonesian
  grammar specifically (smaller, easier to reason about than sena) — e.g. take one real
  affix rule and strip its overt exponent, or raise its `multipleApplication` — so the
  budget/lint tests exercise a realistic grammar shape, not just synthetic toy rules.

## 8. Interaction with the rustify work (PR #446) — and sequencing

**The overlap is near-total.** PR #446's single commit rewrites, among others:
`Morpher.cs`, `Word.cs`, `AnalysisStratumRule.cs`, `SynthesisStratumRule.cs`,
`AnalysisAffixProcessRule.cs`, `AnalysisCompoundingRule.cs`, `AnalysisRewriteRule.cs`,
`ParallelCombinationRuleCascade.cs`, `XmlLanguageLoader.cs`, and `MorpherTests.cs` —
i.e. **every file Layers 1–2 touch**. Beyond textual conflicts:

1. **The budget lives in the hot path rustify just optimized.** Overhead must be measured
   against the *new* engine; a check invisible on master's slower engine could be
   measurable post-rustify. Rustify also deliberately stripped `MorpherStatistics` from
   the hot path — the breach-then-rerun design in §4 exists to honor that decision, and
   should be validated on that engine.
2. **`Word` internals changed** (flat/COW shape, `Pattern<Word, int>` projection, changed
   clone behavior). The `ParseContext` propagation through `Word.Clone` must be written
   against rustify's `Word`, not master's.
3. **Budget defaults need calibration on the shipped engine.** A step budget tuned on
   master would be wildly conservative post-rustify.
4. Even Layer 3 is lightly affected: HC0001/HC0002 inspect `AffixProcessAllomorph.Lhs`,
   whose type changed `Pattern<Word, ShapeNode>` → `Pattern<Word, int>` on rustify.
5. Precedent: the `fst-advisor` branch already stacks on `hc-rustify` and needed a
   mechanical `ShapeNode→int` fix after rebase — the same would happen here, times ten.

**Decided: branch off `hc-rustify` now; do not wait for #446 to merge before starting
implementation.** Rebasing one clean feature branch when #446 lands is routine (already
done once for fst-advisor); writing Layers 1–2 against master and then porting them
across rustify's 100-file rewrite is not. Concretely:

- **Can start now, off `hc-rustify`:** Phase 0 (fixtures/repro harness) and Phase 1
  (budget). Phase 0 is even branch-agnostic (test-only).
- **Layer 3** is nearly independent (reads `Language` structure, never touches the hot
  path) and could start on either base; starting it on `hc-rustify` avoids the one known
  type change (item 4). It's also the natural parallel track if #446 review drags.
- **Do not merge before #446.** Complexity-cap should land *after* rustify to avoid
  forcing a painful rebase onto the 100-file rustify branch. Version-wise this fits the
  already-recommended major-version release train for rustify (master is at 3.9.0;
  rustify targets a major bump); complexity-cap's additive API rides the same train.

## 9. Phases

| Phase | Deliverable | Depends on | Est. size |
|---|---|---|---|
| 0 | Branch off `hc-rustify`. Baseline `indonesian`/`sena` on rustify (max steps/time observed → derive generous `MaxParseSteps`/`ParseTimeout` defaults); build 1–2 pathological variants of the indonesian grammar; repro harness | `hc-rustify` | S |
| 1 | `ParseContext`, `MaxParseSteps` + `ParseTimeout`, soft-stop checks, `ParseDiagnostics` overload, breach re-run with per-rule counters | 0 | M |
| 2 | `MaxRuleApplicationsPerWord`, `MaxAnalysisShapeGrowth`, cascade depth cap | 1 (shares `ParseContext`) | M |
| 3 | `GrammarAnalyzer` + HC0001–HC0008, CLI, "Writing performant HC grammars" guide | — (parallelizable) | M–L |
| 4 | FieldWorks follow-ups: worker DTO status field, FLEx "diagnose word" + parser-report lint surfacing, set conservative caps in HCLoader | 1–3, FW repo | separate effort |

## 10. Open questions

**Resolved 2026-07-02:**

1. ~~Soft-stop vs. throw~~ — **soft-stop**, no exception path for budget/timeout
   exhaustion (§4.4). Real errors still throw as today.
2. ~~Default values~~ — **generous default, shipped on**, not opt-in (§4.1). Exact
   numbers derived from Phase 0 baselining against `indonesian`/`sena`, not guessed.
5. ~~Sample grammars~~ — **use `indonesian-hc.xml`/`sena-hc.xml` directly** as the
   calibration, no-regression, and overhead-benchmark corpus (§7). Provenance/license
   check before permanent commit still applies, but the *design* decision to use them
   (rather than build a separate synthetic-only corpus) is made.

**Still open:**

3. **Rename/deprecate `MaxUnapplications`?** Its name collides conceptually with the new
   caps; same-release cleanup vs. leave-as-is.
4. **Where does `ParseDiagnostics` surface in machine.py parity?** machine.py has its own
   HC port; decide whether these knobs/codes should be mirrored there (same codes would
   keep the guide tool-agnostic).
6. **HC0004/HC0008 precision**: self-feeding/cycle detection via unification is
   approximate; acceptable false-positive rate for a Warning? Start conservative
   (high-confidence patterns only), widen with field feedback.
