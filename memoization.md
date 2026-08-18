# Memoization for HermitCrab analysis — minimal, verified port to master

**Branch:** `feature/memoization` (at master `d9deb167`).
**Goal:** capture the prototype's demonstrated speedup (a pathological Bantu-template
word ~5×; a 312-word Sena heavy set 1,051s → 388s) with a small, human-reviewable diff,
no RUSTIFY rearchitecture, and stronger verification than the prototype had.

Real grammar/corpus content (Sena, Amharic, Indonesian) is never committed to this repo,
including individual word forms in prose — this doc refers to specific test words only by
generic labels (heavy word H1, H2, ...) rather than by their actual spelling.

## 1. Provenance and evidence base

The design is not new. It was built, measured, and corpus-verified on the
`parse-optimization` branch (2026-07-02/03), which was stacked on the unmerged RUSTIFY
commit and later deleted; the full 23-commit chain has been recovered and preserved as the
local branch **`parse-optimization-archive`**. The Rust port (PanGloss `hc-memo`,
`hc-rules/stratum.rs`) faithfully re-implements the same design and adds test patterns and
two hygiene lessons, but no new architecture. Primary references:

- `parse-optimization-archive` — `AnalysisScope.cs`, `AnalysisStateKey.cs`,
  `MemoizedCombinationRuleCascade.cs`, `Word.ReplayOnto` (commits `3522fc41` Phases 0-3,
  `6ea0536a` Phase 3b), plus `parse-optimization.md` and
  `docs/hermitcrab-parse-algorithm-analysis.md` in that tree.
- PanGloss — `hc-memo/src/lib.rs`, `hc-rules/src/stratum.rs:751-955`,
  `hc-rules/tests/memo_gate.rs`, `hc-parse/tests/simultaneous_conformance.rs`.
- fst-advisor branch docs — verification-gate methodology (golden signature dumps,
  per-grammar gating order, soundness batteries).

**Where the speedup actually is (fair sequential baselines, measured on the archive):**

| Mechanism | Effect |
|---|---|
| mrule-cascade positive memo + nogood cache (Phases 2/3) | ~10% (30.5s → 27.4s on heavy word H1). 2,555 real expansions vs 118,162 hits — hits are cheap because guard clauses already reject fast. |
| **template-battery memo (Phase 3b)** | **The real win: 93% of instrumented wall time.** 38,840 battery invocations vs ~2,581 unique keys. H1 27.4s → 6.1s; H2 102.7s → 26.9s; Sena 312-set 1,051s → 388s. |

Both mechanisms share one key type and one replay primitive, so the minimal port includes
both; the template memo is the piece that must not be dropped if scope is ever cut.

## 2. What the design is (one page)

All memoization is scoped to a single `Morpher.ParseWord` call and applies only to the
**sequential** Unordered-order (template/Bantu) analysis cascade.

- **`AnalysisScope`** — carrier object threaded through `Word` clones like `CurrentTrace`
  (reference-shared; excluded from `FreezeImpl`/`ValueEquals`). Holds two tables +
  in-flight guards + a 100,000-entry growth cap (OOM guard; correctness-neutral):
  - `Memo` — mrule-cascade results (empty list = nogood, non-empty = positive),
  - `TemplateMemo` — affix-template-battery results (separate table: same key space,
    different computation),
  - `InProgress` (+ a separate `TemplateInProgress`, a Rust-side clarity improvement worth
    adopting) — re-entry guards; a hit falls through to unmemoized expansion for that call.
- **`AnalysisStateKey`** (readonly struct) — `(Shape, Stratum, SyntacticFeatureStruct,
  RealizationalFeatureStruct, NonHeadCount, multiset of per-rule unapplication counts)`.
  Order-invariant by construction: the multiset collapses the k! orderings that cause the
  98.4% expansion redundancy. Hash uses cached frozen hashes + commutative XOR over the
  multiset; equality compares full values (never trust a bare hash — entries store the full
  key). The constructor **freezes shape/FSs defensively on read** (known landmine:
  `AnalysisAffixTemplateRule.Apply` reassigns an unfrozen `SyntacticFeatureStruct` after
  the Word is frozen; that setter has no `CheckFrozen()` guard).
- **`MemoEntry`** — `{ Results: IReadOnlyList<Word>, MruleTrailPrefixLength,
  NonHeadPrefixLength }`.
- **`Word.ReplayOnto(query, mruleTrailPrefixLen, nonHeadPrefixLen)`** — the replay graft:
  clone the stored result, replace the trail/non-head *prefix* with the query node's own,
  keep the suffix, re-freeze. Sound because everything strictly inside a memoized subtree
  is a deterministic function of the key fields alone; only the two ordered structures the
  key collapses to counts need grafting.
- **`MemoizedCombinationRuleCascade`** (mrule cascade) and an `ApplyTemplateBattery`
  hook in `AnalysisStratumRule` (template battery): both do key → `TryGetValue` → replay
  on hit / compute + `TryAdd` on miss.
- **Tracing bypasses the memo entirely** (`ParseWord` only installs `AnalysisScope` when
  `!IsTracing`): traces must stay byte-identical to the unmemoized engine.

**Design rules locked in from the research (each maps to a specific finding):**

1. **No pruning gate may be coupled to memo presence.** The prototype bundled the Phase-5
   `HasReachableRoot` lexical gate into the memo-on path; Rust deliberately removed the
   coupling to preserve `memo-on == memo-off` as a true invariant. We port the memo only —
   no Phase 5, no gating — so the toggle is a pure optimization knob.
2. **Key-completeness is the primary correctness risk** (packrat/Ford; Bazel cache
   doctrine). The port includes a written audit: every field read by any
   `MorphologicalRules/Analysis*.cs` rule is either in the key or provably irrelevant
   (`IsPartial` and `_isLastAppliedRuleFinal` are grep-verified unread). This audit lives
   as a code comment on `AnalysisStateKey` and must be re-run when analysis rules change.
3. **Trail push and multiset increment are one atomic unit.** `record`-style bookkeeping
   pairing (`_mruleApps.Add` + unapplied-count increment) must never drift; replay's
   prefix/suffix split depends on multiset-equality ⇒ prefix-length-equality.
4. **Budget interaction (future):** if complexity-cap ever lands, a budget-interrupted
   subtree must never be written to the memo ("exhausted subtree is not memoized" — the
   PanGloss ground rule). Not needed now (master has no budgets) but recorded as a standing
   invariant next to the cap constant.
5. **Per-parse lifetime, pre-sized where cheap** — no cross-word or process-global caching.
   (A word→analysis-set cache above the engine is sound and orthogonal — explicitly out of
   scope here; see fst-advisor `HYBRID_FST_FEASIBILITY.md` §6.)

## 3. What master is missing (the port deltas)

Master's HC tree is unchanged since the RUSTIFY fork point (`78350670`), so the substrate
the memo needs — `Word._mruleApps`/`_nonHeadApps`/`Clone`/`Freeze`,
`Shape/FeatureStruct.GetFrozenHashCode/ValueEquals`, `CombinationRuleCascade` — exists
**byte-identical** on master. Deltas:

| Delta | Adaptation |
|---|---|
| `TOffset` is `ShapeNode` on master, `int` on RUSTIFY | Mechanical: port against `IRule<Word, ShapeNode>` etc. |
| **Master has no runtime sequential/parallel toggle** — cascade choice is `#if SINGLE_THREADED`, which no csproj defines, so the shipped library always runs the parallel cascade and the memo path would be unreachable | **Prerequisite work** (the only genuinely new work): add a runtime knob to `Morpher` (RUSTIFY's `maxDegreeOfParallelism` ctor param is the reference design). Default preserves current parallel behavior. Scoped to the analysis side only — `SynthesisStratumRule` is untouched; synthesis has no equivalent memo. |
| No COW `Shape`/`FeatureStruct` (RUSTIFY-only) | Omit Phase 10a-1/7b polish. `ReplayOnto` does a real deep clone on master — slower than the prototype, never wrong. Expect somewhat less than the prototype's exact numbers; measure. |
| `InstrumentedRule`/rule-stats infra, `TrailDirectedRuleCascade` (Phase 0/1), `hc batch` command | Not load-bearing for the memo. Skip from the shipped diff; a slim corpus-signature harness is built as test-only tooling instead. |
| Master's `.Distinct(FreezableEqualityComparer)` in `AnalysisStratumRule` | Keep (harmless with the memo's deduped output; removal is a separate evaluation). |

## 4. Implementation structure

Built and gated in five logical stages (not five separate PR commits — the shipped diff
is squashed to one):

**Stage 1 — prerequisite: runtime sequential-cascade toggle.**
`Morpher` gains `MaxDegreeOfParallelism` (ctor param; default = current parallel
behavior). `AnalysisStratumRule` selects sequential vs parallel cascade at runtime
instead of `#if SINGLE_THREADED`. No memoization yet. Gate: full unit suite green;
behavior byte-identical at default; a test pins sequential == parallel output on an
existing grammar.

**Stage 2 — key + scope + replay primitives, with their unit tests.**
New `AnalysisStateKey`, `AnalysisScope`; additive `Word` members (`ReplayOnto`,
`UnappliedRuleCounts`, `MorphologicalRuleTrailLength`, `AnalysisScope` property).
Nothing consumes them yet. Tests ported from the archive + PanGloss `hc-memo` unit
battery: key order-invariance over permuted unapplication sequences, hash consistency,
sensitivity to non-head count and differing multisets, replay prefix/suffix graft,
in-flight guard semantics. The key-completeness audit comment lands here.

**Stage 3 — mrule-cascade memo (positive + nogood).**
`MemoizedCombinationRuleCascade` wired into `AnalysisStratumRule`'s Unordered sequential
path; `ParseWord` installs `AnalysisScope` when sequential and `!IsTracing`. Tests:
hit-counter-guarded equivalence test (compounding grammar, from archive `MorpherTests`),
plus the **new adversarial fixtures the prototype never had** (§5, items a-b).

**Stage 4 — template-battery memo (the 5×).**
`TemplateMemo` + `ApplyTemplateBattery` in `AnalysisStratumRule`. Tests: the
affix-template equivalence test (two commuting prefix rules, hit-counter-guarded), and
the memo-on/off gate exercising both tables in one parse (PanGloss `memo_gate.rs` shape).

**Stage 5 — corpus verification harness + benchmark evidence.**
Test-only `[Explicit]` corpus runner in the HC test project (modeled on
`FstSenaBenchmark.cs`: env-var driven, per-word watchdog), comparing canonical
analysis-set signatures against uncommitted local grammars. Results recorded in §5/§6.

Ship the memo **on by default whenever the cascade runs sequentially** (it is a pure
optimization once the invariant tests pass); the *default cascade mode* stays parallel in
this PR — flipping the library default to sequential+memo (which the corpus numbers say is
faster anyway) is proposed as a follow-up PR with its own benchmark evidence, so the
behavior-default change and the mechanism land as separately revertable units.

## 5. Verification plan (stronger than the prototype's)

Per-commit unit gates as above, plus:

- **Memo-on/off analysis-set equality as the standing acceptance gate** (the metamorphic
  relation: equal keys ⇒ equal result sets) — **not byte-for-byte object equality.** The
  gate compares canonical signature sets (sorted `join("+", morphemeIds)+":"+rootIndex`),
  because `ReplayOnto`'s grafted `Word` is not guaranteed field-for-field identical to a
  freshly-computed one (e.g. `ShapeNode`/annotation object identity differs) even when it
  represents the same analysis. Every equivalence test asserts non-vacuously (memo hit
  counters > 0, both tables non-empty where applicable).
- **(a) SelfOpaquing ≥2-iteration fixture — the confirmed-bug shape (resolved: not reproduced,
  wired in as a standing regression).** PanGloss's rust conformance suite documents a
  confirmed C#-oracle bug in this exact code path (a `RewriteApplicationMode.Simultaneous`
  epenthesis rule against root 19's `"b+ubu"`; `AnalysisRewriteRule` compiles this shape as
  `ReapplyType.SelfOpaquing`, a repeat-until-fixpoint loop). Before trusting stage 3, this
  was reconstructed directly against `parse-optimization-archive`'s own `AnalysisScope`
  using the real `RewriteRuleTests.EpenthesisRules` rule definition: parsing `"buibui"`
  gave the same result (1 parse) under every tracing/call-order combination tried
  (non-traced, traced, repeated, order-swapped) — no divergence. The real minimal fixture
  (`conformance/rewrite/simultaneous-epenthesis/`) is not present in this checkout to test
  against directly (its own Rust test is `#[ignore]`'d for the same reason). Per the
  advisor consult: this does not block stage 3 — the general memo-on/off analysis-set
  equality gate is strictly stronger than one fixture, and PanGloss's own Rust analysis
  reached the identical conclusion for their side (memo-sound on this shape; the ≥2-iteration
  loop×memo interaction specifically "remains untested" because no available fixture drives
  the loop past one iteration). **Shipped:** `MorpherTests.ParseWord_MemoOnMatchesMemoOff_
  ForSelfOpaquingSimultaneousEpenthesis` wires this exact fixture in as a standing
  regression case (memo-on/off equality plus a pinned-value assertion), and the
  ≥2-iteration gap is recorded here verbatim as an open, honest limitation — not silently
  dropped.
- **(b) Trail-order-observable grammar — closed at the right layer, not through `ParseWord`.**
  `Morpher.ParseWord` does not return raw analysis-cascade words — it feeds them through a
  full **synthesis** re-derivation (`Synthesize`/`LexicalLookup`/`PermuteRules`), which
  re-explores rule orderings on its own, so a `ParseWord`-level probe is the wrong
  instrument for this: an attempt via a disabled-merge grammar variant returned 0 results
  (most likely `MergeEquivalentAnalyses=false` breaking that synthesis path some other way,
  not evidence about trail-order observability one way or the other — not chased further,
  since it isn't the right layer regardless). **What the actual safety net is:** the
  standing memo-on/off analysis-*set*-equality gate catches the class of bug that matters
  — a dropped or spuriously-added analysis, exactly the PanGloss 0-vs-1 shape — because a
  dropped analysis never reaches synthesis to be re-normalized away. Internal analysis
  trail order, by contrast, is plausibly a don't-care for the returned output set, which
  is also why it doesn't need a `ParseWord`-level test. The graft primitive itself IS
  pinned by a real red/green test, at the layer where it's actually observable:
  `AnalysisStateKeyTests.ReplayOnto_Grafts*` (stage 2, direct construction) and
  `MemoizedCombinationRuleCascadeTests.Apply_PositiveReplayMatchesUnmemoizedResultSet_
  IncludingTrailOrder` (stage 3, a synthetic 3-rule cascade run directly against
  `MemoizedCombinationRuleCascade` — bypassing `Morpher` so a real commuting-order replay
  can be forced — comparing the memoized run's result set against an unmemoized run
  *including* `MorphemesInApplicationOrder`, i.e. trail order). Verified this actually
  discriminates: temporarily breaking `ReplayOnto` to skip the prefix graft made both this
  test and the stage-2 unit tests fail, confirming they are not vacuous.
- **Corpus gates, in the fst-advisor order:** Indonesian 121/121 first (cheap iteration);
  guarded Sena slice (first 60 words, 5s/word watchdog); then the Sena 312-word heavy set.
  Compare sequential-memo (`maxDegreeOfParallelism: 1`, the only sequential configuration
  that exists post-stage-3 — the memo is unconditional whenever the cascade is sequential,
  so "sequential-unmemoized" is no longer a reachable end-to-end configuration; that
  comparison is instead covered at the unit level, see §5(b)) against parallel-unmemoized
  master default — the actual user-visible claim. Must be **analysis-set identical** — same
  canonical signature set, not byte-identical objects — including negative/no-parse words
  (nogoods cache no-parses too — both sides must produce the empty set). Amharic
  (machine-local, gitignored) as the alphabet-stress smoke test; never commit real-grammar
  fixtures (privacy constraint).
- **Reporting** (standing requirement): every measured claim ships with p50/p95 per-word
  latency, aggregate wall, and hit/miss counts (`DiagMemoHits`/`DiagNogoodHits`, split —
  not just totals) — and stage 5 must assert `DiagMemoHits > 0` somewhere in the corpus
  run, so the positive-replay path can't ship having never actually fired end-to-end.

**Stage 5 result (local uncommitted grammar — Sena, both slices):** ran
`MemoCorpusVerification` against the local `samples/data/sena-hc.xml` /
`sena-words.txt` (never committed — grammar-privacy constraint).

| Slice | Timeout | Compared | Timed out | No-parse (both) | Divergences | Mrule memo (pos/nogood) | Template memo (pos/nogood) |
|---|---|---|---|---|---|---|---|
| First 60 | 5s | 54 | 6 | 11 | **0** | 8,652 / 59,156 | 10,481 / 3 |
| Full 312 | 8s | 252 | 60 | 87 | **0** | 201,051 / 1,692,696 | 286,581 / 29 |

Zero divergences across both runs, hundreds of thousands of memo hits on both
tables (not vacuous), on real analysis-rule content the toy-grammar unit tests
(stages 2-4) structurally cannot reach — this is the empirical confirmation
of `AnalysisStateKey`'s key-completeness audit. The full-312 run's timeouts
(60 words, 8s/side budget) are the same known pathological Bantu template
words the archive prototype measured; p50 678.7ms / p95
7,997.2ms per word (memo-on + memo-off combined) on the successfully-compared
words. Full per-word timing table is in the run's `TestContext` output, not
committed (never commit real-word signature data).

**Indonesian (121/121, PanGloss's local `indonesian-hc.xml`/`-words.txt`):**
**0 divergences**, 0 timeouts, p50 37.2ms / p95 346.0ms. Mrule memo: 64
positive / 157 nogood. Template memo: **0 positive / 410 nogood** — Indonesian
has no Bantu-style affix-template redundancy, so the template table only ever
proves subtrees empty here, never replays; matches §6's own prediction that
typical (non-template) grammars see little upside. Per-heavy-word timing on
the 10 slowest words shows memo-on (sequential) is actually *slower* than
memo-off (parallel default) by ~10-15% on this grammar — expected and
consistent with §6: the memo only pays for itself where order-variant
redundancy exists, and sequential-single-core loses to parallel-multi-core
when it doesn't. This is the honest "no regression in the wrong direction on
non-Bantu grammars" finding, not a null result — the comparison being measured
here is user-visible (sequential-memo vs parallel-unmemoized default), not the
memo mechanism isolated from single- vs multi-threading (see §5's note on why
"sequential-unmemoized" isn't independently constructible once the mrule-cascade
memo is wired in).

**Amharic (machine-local, gitignored, alphabet-stress smoke test):** 30 words,
8s/side timeout. **0 divergences** on the 6 words that finished both sides
within budget (10 timed out — Amharic's Ge'ez-script grammar is known to be
build/parse-heavy; 14 had no parse on either side). Mrule memo: 14 positive /
113 nogood. Template memo: 41 positive / 2 nogood — both fired. Confirms the
key/replay machinery is script-agnostic (Ge'ez abugida segments, not Latin
transliteration) with no divergence. No Amharic word forms appear in this doc
or any committed file, per the standing grammar-privacy constraint.

**Summary across all three real grammars tested: zero divergences, in every
case with both memo tables exercised non-vacuously.** This is the strongest
available evidence for `AnalysisStateKey`'s key-completeness audit short of
running the entire, much larger production corpora.

**Critical follow-up (advisor-flagged): the aggregate corpus runs above prove
nothing about the heavy words, because they timed out and were excluded from
the equality gate.** 60 of the full-312 Sena run's words never got compared —
those are exactly the template-heavy words the memo exists for, and the ones
where a key-completeness miss would actually surface. A short timeout also
means the only completed timings showed memo-on *slower* (Indonesian,
Amharic) — sequential-memo vs parallel-unmemoized, not evidence of the actual
speedup goal. Closed with a targeted, longer-timeout run on heavy word **H1**
(known-pathological, present in the Sena heavy-word set) at 240s:

- **Soundness on a heavy word, actually executed:** memo-on vs memo-off —
  analysis-set identical (0 divergences). Mrule memo: 29,736 positive /
  88,426 nogood hits. Template memo: 37,512 positive / 0 nogood hits — this
  is the key-completeness confirmation the excluded corpus runs above could
  not provide.
- **User-visible timing:** memo-on (sequential+memo) **22.1s** vs memo-off
  (today's shipped parallel default) **136.1s** — **6.2×**.
- **Isolated memo contribution** (mutation-tested: temporarily disabled the
  `AnalysisScope` install in `Morpher.ParseWord`, reverted after — same
  technique as the ReplayOnto mutation test in §5(b)): sequential-no-memo
  **138.0s** vs parallel-no-memo (unchanged) **141.5s** — parallelism alone
  contributes essentially nothing on this word (~2%). Compared against
  sequential-**memo**'s 22.1s: the memo mechanism itself is responsible for
  essentially the entire **~6.3× speedup**, not a threading artifact.

This exceeds the plan's own target (≥3× on heavy-word-class words) despite
master lacking the prototype's copy-on-write `Shape`/`FeatureStruct` — the
`ReplayOnto` deep-clone tax feared in §6 below did not dominate here.

**Second heavy word, H2 — soundness inconclusive, not a clean win.** Two
independent runs at a 400s per-side timeout both had the memo-on
(sequential+memo) side fail to finish within 400s — i.e. this word is *not*
analysis-set-verified at this depth; the "Passed" result in the first run
reflects the harness's by-design exclusion of timed-out words from the
divergence check, not a completed comparison. This is a materially
different outcome from H1 and is reported honestly rather than folded into
the "0 divergences" summary above. Notably, the archive prototype's own
measurement (§1's table above) had H2 at 102.7s → 26.9s with the template
memo — master's ported memo-on run did not complete this word within 400s,
~15× slower than the archive's memoized figure. Plausible explanations
(unconfirmed): master's deep-clone `ReplayOnto` (vs the archive's
copy-on-write `Shape`/`FeatureStruct`) may cost materially more on this
word's particular redundancy shape than it did on H1; or H2 exercises a
rule-graph region where the memo's hit rate is lower and raw expansion
dominates. Not chased further with a longer timeout, per the same
bounded-effort judgment applied to the other unmeasured heavy words below —
documented as unmeasured/slower-than-expected, a concrete follow-up, not a
blocker for this PR (soundness is unaffected either way: an incomplete run
makes no claim, positive or negative, about key-completeness for this
word).

Remaining honest gap: H1 is the one heavy word with a completed, positive
measurement; H2 is a heavy word with an *incomplete* one (see above); the
other 58 timed-out full-312-run words (and Indonesian/Amharic's timed-out
words) are unmeasured at this depth — a natural follow-up, not a blocker,
given the confirmed pattern (soundness holds, memo dominates the win) on
the one word measured to completion.

**Aggregate corpus evidence: count-based and wall-clock, reported
separately (see the harness's own two-line report format).** A Sena
sub-corpus (words 1-70, 200s per-side timeout — long enough for the one
tractable heavy word in this range to complete on both sides, per §5's H1
measurement above) gives, of the 69 words that completed within budget:
**53/69 (77%) faster under memo, 16/69 (23%) slower** — count-based, this
is the "yes, some words are slower" half of the picture. **Wall-clock:
memo-on total 114.6s vs memo-off total 277.8s across those same 69
words — 2.42× faster in aggregate** — this is the "but a lot are faster,
and it nets out strongly positive" half. The one word that timed out at
200s (a second tractable-but-slow heavy word, distinct from both H1 and
H2) is excluded from both numbers. The harness's timeout wraps both the
memo-on and memo-off calls in one try block per word, so it cannot record
which side actually timed out for this word — if it was memo-on (the more
likely case here, since it's attempted first and this class of word is
exactly where the memo's own cost is least understood, see the H2
discussion below), the word's true memo-off time is genuinely unmeasured,
not "presumably at least as slow." **This 2.42× figure should be read as
this slice's measured result, not asserted as a guaranteed lower bound**
in either direction — a longer timeout is the only way to actually find
out. Both memo tables fired heavily and non-vacuously on this slice
(mrule: tens of thousands of positive/nogood hits; template: tens of
thousands positive, near-zero nogood).

**Is the typical-word slowdown "just" losing a thread? Mostly, not
purely.** Isolated on Indonesian (no template redundancy — the cleanest
instrument for this, since the template memo never replays there) via
three repeated runs per configuration to average out run-to-run JIT/GC
noise (single-run absolute times varied by as much as 60% between
otherwise-identical runs of the unchanged parallel baseline — only
within-run memo-on-vs-memo-off ratios are trustworthy here, not
across-run absolute milliseconds):

| Configuration vs parallel-default | Aggregate ratio (3 reps) | Implied slowdown (1/ratio − 1) |
|---|---|---|
| sequential + memo (shipped opt-in) | 0.80×, 0.80×, 0.78× | ~25-28% |
| sequential, memo forced off (mutation-tested) | 0.69×, 0.80×, 0.68× | ~25-47% |

The sequential-*no-memo* slowdown is consistently at or above the
sequential-*memo* slowdown, never below it. So most of the typical-word
regression genuinely is the lost thread (both configurations are slower
than parallel-default, by a similar order of magnitude) — but it is not
*purely* that: even on a grammar where the template memo never once
replays, the mrule-cascade's own cache still fires (64 positive / 157
nogood hits on this corpus) and measurably claws back some of the
thread-loss penalty rather than adding to it. Three reps each is enough to
see the direction of the effect, not enough to state a precise recovered
percentage with confidence — a larger repeated-trial run would sharpen
this if the exact number ever matters for the default-flip decision.

**Is the H2 shortfall actually the deep-clone tax?** Plausible, not
confirmed — resist stating it as established. The one piece of direct
evidence is that H2's memo fired heavily before timing out (tens of
thousands of positive hits on both tables, §5 above) — the memo is
clearly not idle on this word, which favors "replay/clone cost per hit is
what's expensive" over "the memo doesn't engage." But that is a
directional tell, not a measurement: distinguishing "expensive replay" from
"just a lot more raw expansion than H1" requires profiling where memo-on
wall time actually goes (`Clone`/`Freeze`/`ReplayOnto` vs raw rule
expansion) on H2 specifically. Scoped as a follow-up, not undertaken here.

## 6. Expected outcome and honest caveats

- Pathological template-heavy words: prototype showed ~5× (with COW clones). Master's
  deep-clone `ReplayOnto` was expected to give back some of that; **measured directly on
  heavy word H1 (§5 addendum): ~6.3× isolated memo contribution, ~6.2× on the
  user-visible sequential-memo-vs-parallel-default comparison — met and exceeded the
  ≥3× target**, meaning the feared `ReplayOnto` deep-clone tax did not dominate on this
  word. The ≥50% Sena-heavy-set aggregate target remains unmeasured (that run's heavy
  words timed out before completing the equality gate, §5 addendum) — a natural
  follow-up. A second heavy word, H2, did NOT complete within a 400s memo-on budget (§5
  addendum) — the first concrete (if inconclusive) counter-data-point to the
  deep-clone-tax question, since the archive's COW-based prototype handled this same
  word in 26.9s. If a future heavy-set measurement shows replay cost dominating after
  all, this is the evidence that would motivate porting `Shape` COW as its own
  follow-up — do not fold it into this PR pre-emptively.
- Typical words (Indonesian-class) get measurably *slower* under sequential+memo
  (~25-28% in aggregate, §5 addendum) — mostly the cost of losing a thread, not the memo
  itself (isolated via a sequential-no-memo comparison, same addendum). The corpus gate
  proves "no divergence" there, not "no regression" — this is exactly why the memo ships
  off by default rather than flipping the library's default cascade mode.
- The memo is inert for `Ordered`-strata-only grammars and for `ParallelCombinationRuleCascade`
  (no natural subtree-completion moment in its breadth-first walk — same reason the
  prototype left it unmemoized).

## 7. Out of scope (deliberately)

Phase 0/1 instrumentation and synthesis-side `TrailDirectedRuleCascade`; Phase 4/5 gates
(measured inert or unsound-coupled); RUSTIFY COW polish (7b/10a); packed forest (9c);
word-level result caching above the engine; any interned-ID key redesign (`rust-conversion.md`
§6.3 is an unshipped plan — needs its own measurement if ever wanted).
