# HermitCrab engine internals: performance and complexity gotchas

Part of the [HermitCrab-for-LLMs reference](README.md). Repo: `sillsdev/machine`.

## What this covers

Ways a HermitCrab grammar can parse (or fail to parse) pathologically slowly for reasons that
live in the **engine**, not in anything visible from reading the grammar's own XML in isolation.
Each entry here is grounded in a fixture from this repo's own conformance test suite
(`conformance/edge-cases/` and `conformance/languages/`), where the grammar's author wrote a
comment naming the exact pathology the fixture pins on purpose, plus a trace into the C# engine
source that actually causes it. These are not hypothetical: every fixture cited below is a real,
minimal grammar checked into this repo specifically to keep this behavior pinned and monitored
(`docs/archive/conformance-framework-plan.md` §4.5/§7 calls these "complexity drivers").

This doc assumes you already have (or can get) a grammar exported as HermitCrab XML — see
[`getting-started.md`](getting-started.md) if not.

## Quick reference

| Gotcha | Primary files (fixture + engine) | One-line description |
|---|---|---|
| Deep-optional-affix-nesting / template-slot backtracking | `conformance/edge-cases/deep-optional-affix-nesting/grammar.xml`; `SynthesisAffixTemplateRule.cs` (`ApplySlots`), `AffixTemplateSlot.cs` | `n` independently-optional affix-template slots recurse into the full `2ⁿ` "fired/skipped" powerset — full depth in [`affix-templates-and-optionality.md`](affix-templates-and-optionality.md). |
| Disjunctive allomorph over-generation + deferred recheck | `conformance/edge-cases/disjunctive-recheck/grammar.xml`, `conformance/edge-cases/strrep-identity/grammar.xml`; `SynthesisAffixProcessRule.cs:195-227`, `Allomorph.cs:80-156`, `Word.cs:464-470` | An environment-constrained (non-elsewhere) allomorph doesn't stop the synthesis loop from also trying later allomorphs — every one produces a live candidate word, and the correct one is picked only later, by a second full pass over all of them. |
| Iterative self-feeding phonological-rule cascade | `conformance/edge-cases/simultaneous-epenthesis-cascade/grammar.xml`; `IterativePhonologicalPatternRule.cs:17-48`, `EpenthesisSynthesisRewriteSubruleSpec.cs:23-43`, `InfiniteLoopException.cs` | An `Iterative` epenthesis rule with no filter against its own output can re-match the segment it just inserted, forever, until a hardcoded 256-node cap throws `InfiniteLoopException`. |
| One crashing word aborts the whole batch | `conformance/edge-cases/simultaneous-epenthesis-cascade/words.yaml`; `SignatureFormat.cs:31-44` (`SIL.Machine.Morphology.HermitCrab.Tool`) | `InfiniteLoopException` (and any exception except `InvalidShapeException`) propagates out of per-word parsing and kills the entire word-list run, not just that word. |
| Compounding split-point enumeration | `AnalysisCompoundingRule.cs:39-51,54-124`, `SynthesisCompoundingRule.cs` (`AllSubmatches = true` matcher, `_rule.MaxApplicationCount`), `Morpher.cs:57` (`MaxStemCount`) | Analysis-direction compounding tries every way to split a word's shape into head+non-head at every stratum; `Morpher.MaxStemCount` (default 2) is the direct lever that keeps this from recursing into unbounded N-ary splits. |

## Deep-optional-affix-nesting / template-slot backtracking

Full mechanism, code walkthrough, and the linear-cost fix are already written up in
[`affix-templates-and-optionality.md`](affix-templates-and-optionality.md) — read that file for the
complete account of `SynthesisAffixTemplateRule.ApplySlots`'s recursive per-slot batching and why an
all-optional slot chain explores the full `2ⁿ` "fired vs. skipped" powerset.

What to add here is the terminology bridge: `docs/archive/conformance-framework-plan.md` §7 lists
this complexity driver under **two** names — "deep-optional-affix-nesting combinatorics" and
"template-slot backtracking" — because they're the same mechanism seen from two angles.
`ApplySlots`'s recursion (try the slot, recurse; if optional, *also* recurse past it using the
un-modified input) is literally a textbook backtracking search over "which slots fired"; the
`deep-optional-affix-nesting` fixture is what that backtracking search looks like when every slot is
independently optional and no slot's rule ever fails to match. If you see either term in this repo's
docs, plan, or fixture comments, they name the same code path.

The fixture itself — `conformance/edge-cases/deep-optional-affix-nesting/grammar.xml` — is 12
independent `optional="true"` prefix slots (`mrP1`..`mrP12`), each inserting the literal character
`x`. Its `words.yaml` records the confirmed blowup directly: a word with 6 leading `x`'s has exactly
`C(12,6) = 924` equally-valid analyses (all byte-identical surface text, distinct morpheme chains),
oracle-confirmed and checked in as 924 individual `parses:` entries. Because this fixture is
deliberately expensive, it carries `budget_ms: 15000` in its `words.yaml` front matter and is
**excluded from the default conformance run** — the harness only exercises it with
`--include-pathological` (`conformance/README.md`, "Running it").

## Disjunctive allomorph over-generation and deferred recheck

**What it is.** A morpheme with several allomorphs, where more than one allomorph is
environment-constrained (not the unconstrained "elsewhere" case), causes the *synthesis* search to
carry more live candidate words forward than the final grammar will actually accept — the rejection
happens later, in a second full pass, not by narrowing the search up front.

**The fixture.** `conformance/edge-cases/disjunctive-recheck/grammar.xml` builds this on purpose. Its
`ruleD` (`d-suffix`) has two subrules: `msubD0` ("-za") is environment-constrained
(`RequiredEnvironments`: must follow a `k`), and `msubD1` ("-da") is the unconstrained elsewhere
case. The grammar's own comment on `ruleD` explains the mechanism precisely:

> Subrule 0 ("-za") is environment-constrained ..., so C#'s synthesis loop does NOT break after it
> (`SynthesisAffixProcessRule.cs:235-242`) and subrule 1 ("-da", the elsewhere case) also produces an
> output, which records subrule 0 in its passed-over set (`appliedAllomorphIndices`). At final
> validity (`Allomorph.IsWordValid`'s second loop, `Allomorph.cs:127-152`) a "-da" parse is rejected
> whenever subrule 0's environment is ALSO satisfied at the same morph position.

**The engine mechanism, verified in source.** In `SynthesisAffixProcessRule.cs` (the loop over a
rule's allomorphs, read at lines ~195-227 on the current tree): after an allomorph applies, the code
only `break`s out of the loop — stopping earlier alternatives from also firing — when three
conditions all hold: the allomorph isn't the last one, it does *not* free-fluctuate with the next
allomorph (`Allomorph.FreeFluctuatesWith`), it has *zero* `Environments`, and it has an empty
`RequiredSyntacticFeatureStruct`. An environment-constrained allomorph fails the "zero Environments"
condition, so the loop **does not break** — every later allomorph is also tried and also contributes
an output candidate. The actual disjunctive filtering — "did an earlier-indexed allomorph's
environment also match here, in which case this later one shouldn't have been used" — happens only
once the word's full surface form exists, in `Allomorph.IsWordValid` (`Allomorph.cs:105-156`): it
walks `word.GetDisjunctiveAllomorphApplications(morph)` (the passed-over set recorded during
synthesis, `Word.cs:464-470`) and rejects the candidate if any earlier, non-free-fluctuating,
environment-satisfied allomorph should have won instead.

**Why this matters for performance.** For a morpheme with `k` allomorphs where `m` of the first
`k-1` are environment-constrained (not the elsewhere case), synthesis carries up to `m+1` live
candidate words forward from that single rule application — each one fully expanded through every
subsequent affix template slot and phonological rule — before the bulk of them are pruned back out at
final validity. This cost is not exponential by itself, but it **stacks multiplicatively** with every
other rule application downstream of it in the same derivation, and it is invisible from the grammar
alone: nothing in the XML says "this environment gate delays rejection instead of preventing
generation." `conformance/edge-cases/strrep-identity/grammar.xml`'s `rulePfx`/`ruleObj` pins the same
mechanism from a different angle (its own comment calls this the "DISJUNCTIVE-BREAK probe" and
"ENVIRONMENT-RECHECK probe"), and
`conformance/edge-cases/free-fluctuating-allomorph-pair/grammar.xml` pins the escape hatch: when two
allomorphs' constraint sets compare exactly equal (`Allomorph.FreeFluctuatesWith`), the loop treats
them as free variation rather than disjunctive alternatives, and neither is retroactively rejected.

**Mitigation.** This is largely an engine cost a grammar author cannot design around directly (unlike
the affix-template case, there's no "collapse into one slot" fix — this is inherent to how
environment-conditioned allomorphy has to work when the environment isn't knowable until synthesis
produces a surface string). What *is* actionable: minimizing the number of allomorphs per morpheme
that carry a genuine environment constraint (as opposed to using a broader natural class match that
could instead be pushed into a phonological rule later in the derivation), and putting the
unconstrained "elsewhere" allomorph last, keeps the passed-over set small per application.

## Iterative self-feeding phonological-rule cascade

**What it is.** An `Iterative`-mode phonological rule (the DTD default — no
`multipleApplicationOrder="simultaneous"` attribute) that inserts material matching its own trigger
environment can re-match the segment it just inserted on its very next iteration, cascading until a
hardcoded safety cap fires and the engine throws.

**The fixture.** `conformance/edge-cases/simultaneous-epenthesis-cascade/grammar.xml` (named for the
Simultaneous-vs-Iterative *contrast* it probes, not because its own rule is Simultaneous — its rule,
`prule4`, carries no `multipleApplicationOrder` attribute, so it runs Iterative) defines a rule that
inserts an "HFU vowel" (`ncHFUVowel`, e.g. `i`) after any high vowel (`ncHighVowel`), with an empty
`PhoneticInput` (a zero-width LHS — pure epenthesis) and no `RightEnvironment` restricting where it
can fire. Parsing the root `bubu` alone against this grammar is enough to crash the engine. The
fixture's `words.yaml` documents the mechanism and its own `expect_crash: true` outcome — this
fixture is expected to make the oracle **crash**, and a conforming engine must crash the same way for
the fixture to pass:

> Because C#'s epenthesis substitute LHS pattern never adds a Modified=Clean filter,
> `IterativePhonologicalPatternRule.Apply`'s freshly-inserted "i" node is itself a HighVowel and gets
> revisited by the very next iteration of the same outer loop, satisfying its own LeftEnvironment and
> re-triggering insertion — a runaway self-feeding cascade that only stops when
> `EpenthesisSynthesisRewriteSubruleSpec.ApplyRhs`'s own hard cap (`Shape.Count == 256`) throws
> `InfiniteLoopException`.

**The engine mechanism, verified in source.** `IterativePhonologicalPatternRule.Apply`
(`IterativePhonologicalPatternRule.cs:17-48`) is a `while (targetMatch.Success)` loop: it matches,
applies the matched subrule's `ApplyRhs` if one matches, advances a `start` cursor to just past the
matched range, and re-matches from there — repeating until no match remains. For a zero-width
epenthesis match (empty `PhoneticInput`), the newly inserted node sits essentially where the next
match search resumes, and because nothing in this rule's pattern excludes "a segment this same rule
just inserted," a freshly-inserted high vowel is itself a valid left-context for inserting *another*
high vowel right after it. This repeats without bound. The only thing that stops it is a hardcoded
check in `EpenthesisSynthesisRewriteSubruleSpec.ApplyRhs`
(`EpenthesisSynthesisRewriteSubruleSpec.cs:32-33`): `if (targetMatch.Input.Shape.Count == 256) throw
new InfiniteLoopException(...)`. There is no grammar-level knob that changes this cap, and no
graceful degradation — it is a hard crash, by design (`InfiniteLoopException.cs`).

**The contrast that avoids it.** `SimultaneousPhonologicalPatternRule.Apply`
(`SimultaneousPhonologicalPatternRule.cs:22-37`) collects **every** match over the *original,
unmutated* input via `Matcher.AllMatches(input)` first, and only afterward applies every collected
subrule match. Because match collection happens before any mutation, a `Simultaneous`-mode epenthesis
rule can never see, and therefore can never re-trigger on, a segment one of its own sibling matches
is about to insert in the same pass — this is exactly the "Simultaneous-mode epenthesis... two
insertion sites... must not feed each other" behavior already documented in
[`affix-templates-and-optionality.md`](affix-templates-and-optionality.md)'s sibling notes and pinned
by the P13 fixture family this one contrasts against.

**Mitigation.** For any epenthesis (LHS-empty, insertion-only) `RewriteRule` whose inserted segment's
own feature bundle could satisfy the rule's own `LeftEnvironment`/`RightEnvironment`, prefer
`multipleApplicationOrder="simultaneous"` over the default Iterative mode — Simultaneous mode's
"collect all matches against the original input, then apply them all" semantics is structurally
immune to this specific self-feeding cascade. If Iterative mode is required for some other reason
(e.g. a rule that genuinely needs to see its own prior applications to converge on a correct output),
make sure the environment can never be re-satisfied by the rule's own insertion — e.g. by writing the
environment to require a feature the inserted segment doesn't carry.

## Operational corollary: one crashing word aborts the whole batch

This is not a parse-time complexity gotcha but a direct operational consequence of the crash above,
worth knowing before it surprises you in a batch run. `SignatureFormat.ParseOneWord`
(`SignatureFormat.cs:31-44`, in `SIL.Machine.Morphology.HermitCrab.Tool`, the code the `hc` CLI and
this repo's conformance oracle both use for per-word parsing) wraps only a single `catch
(InvalidShapeException)` around `morpher.ParseWord(word)` — its own doc comment says explicitly:
"any other exception propagates, since that's a genuine engine crash, not a normal per-word outcome."
`InfiniteLoopException` is exactly such an exception. Practically: if a word list or a batch parse
run hits one pathological word that trips the 256-node epenthesis cap (or any other engine-level
crash), that exception unwinds out of the per-word try/catch and terminates processing of every
*subsequent* word in the same run — it does not get recorded as a per-word failure and skipped. The
`simultaneous-epenthesis-cascade` fixture's own `words.yaml` deliberately keeps itself to a single
word for exactly this reason ("a crash aborts the whole word loop, so anything after the crashing
word would never actually be exercised"). If you're batch-parsing a large corpus and the run stops
partway with no per-word error for the remaining words, suspect a crash on the word where it actually
stopped, not silent success on everything after.

## Compounding split-point enumeration

**What it is.** Analysis-direction compounding doesn't guess where a compound's head/non-head
boundary is — it tries every position a `CompoundingSubrule`'s head/non-head patterns can match
against the observed shape, at every stratum where a `CompoundingRule` is declared, and only then
filters by whether the resulting non-head substring actually resolves to a real lexical root.

**No dedicated pathological fixture exists for this yet** — `docs/archive/conformance-framework-plan.md`
§6 records `CompoundingRule` as "covered, thin (2 fixtures)," and §7 names "compounding
combinatorics" as one of the five identified complexity drivers, but (unlike the two entries above)
the suite doesn't currently carry a `budget_ms`/pathological-category fixture pinning it. The
mechanism below is grounded directly in engine source, not a fixture comment — treat it as
documented-but-unpinned.

**The engine mechanism, verified in source.** `AnalysisCompoundingRule.Apply`
(`AnalysisCompoundingRule.cs:39-51`) first checks `input.NonHeadCount + 1 >= _morpher.MaxStemCount`
and bails out if the word already has as many stems as `Morpher.MaxStemCount` allows — this defaults
to `2` (`Morpher.cs:57`, set in the constructor), i.e. binary compounds only, unless a caller raises
it. Below that cap, for each `CompoundingSubrule` it runs a `MultiplePatternRule` built with
`AllSubmatches = true` (`AnalysisCompoundingRule.cs:26-34`) — meaning the matcher doesn't stop at the
first way to split the shape into a head-pattern match and a non-head-pattern match, it enumerates
**all** of them. Every one of those split candidates is then checked against
`_morpher.SearchRootAllomorphs` (a trie lookup, `AnalysisCompoundingRule.cs:62`) to see whether the
candidate non-head substring is an actual stored root allomorph — the code's own comment at line
59-60 states the reason: *"for computational complexity reasons, we ensure that the non-head is a
root, otherwise we assume it is not a valid analysis and throw it away."* This root-lookup filter is
already a deliberate mitigation the engine applies for you (it prunes most split candidates
immediately, since most substrings of a word aren't stored roots) — but for a grammar with a large
compounding lexicon and permissive head/non-head patterns (e.g. `OptionalSegmentSequence` spans
rather than tightly anchored shapes), the number of split points examined before that filter still
scales with word length, and — if `MaxStemCount` is raised above its default of 2 to permit N-ary
compounds — each additional stem multiplies the number of split combinations considered, since every
accepted binary split becomes a new `input` that the same rule is tried against again on the next
outer application (bounded by `_rule.MaxApplicationCount` per rule,
`AnalysisCompoundingRule.cs:46`).

**Mitigation.** Keep `Morpher.MaxStemCount` (a host-application/caller setting, not a grammar-XML
attribute) as low as your language's actual compounding depth requires — raising it beyond what's
linguistically needed directly multiplies the split-point search. Within the grammar itself, prefer
`CompoundingSubrule` head/non-head patterns anchored as tightly as the language allows (fixed-length
or feature-narrow contexts) over broad `OptionalSegmentSequence` spans, since a tighter pattern
reduces the number of positions `AllSubmatches` has to enumerate before the root-lookup filter gets a
chance to prune them.

## Tooling: the conformance harness itself as a diagnostic aid

`src/SIL.Machine.Morphology.HermitCrab.Conformance/` (in the `conformance-framework` branch/PR; not
yet on `master` as of this writing) is the C# project backing the fixtures cited above, and it is
itself useful when debugging a slow or misbehaving grammar, independent of any specific gotcha above:

- **`GrammarRuleIndex.cs`** independently re-parses a `grammar.xml` (not through the runtime
  `XmlLanguageLoader`) to recover every declared rule's XML `id` — a mapping the runtime loader
  itself discards for `CompoundingRule`/`PhonologicalRule`/`MetathesisRule` (whose runtime objects
  expose only `IHCRule.Name`) and only partially retains for `MorphologicalRule`/`RealizationalRule`
  (via `Morpheme.Id`, sourced from a separate `<MorphemeId>` element, not the `id` attribute). This
  is what lets the coverage report say *which XML rule* a trace or a `words.yaml` `rules:` entry
  actually refers to.
- **`CoverageReport.cs`** cross-references every fixture's exercised constructs and traced rule
  applications against the full construct checklist and every grammar's declared rule ids, flagging
  **dead rules** (a rule id no word in the suite ever exercises) and zero-coverage constructs. Run it
  with `dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures
  conformance --coverage-report` (writes `fixtures.csv`/`coverage.csv`/`rules.csv`, exits non-zero on
  any dead rule). For a grammar you suspect has runaway complexity from rules that don't actually do
  what you think, this is a mechanical way to confirm which rules are truly live in your word list
  versus declared-but-unreachable.

## Practical summary

| Gotcha | Asymptotic shape | Grammar-visible? | Actionable mitigation |
|---|---|---|---|
| Deep-optional-affix-nesting | `O(2ⁿ)` in slot count | Yes — the affix template's own `optional` flags | Collapse independent optional slots into one non-optional, multi-rule slot (see linked doc) |
| Disjunctive allomorph recheck | Multiplicative per environment-constrained allomorph | No — invisible from the grammar alone | Minimize non-elsewhere allomorphs per morpheme; keep the unconstrained allomorph last |
| Iterative self-feeding cascade | Runs to a hard 256-node cap, then crashes | Partially — visible only if you reason about whether an insertion rule's own output can re-satisfy its trigger | Use `multipleApplicationOrder="simultaneous"` for epenthesis rules whose output could feed themselves |
| Batch-abort on crash | N/A (operational, not algorithmic) | No | Isolate suspect words; don't assume a truncated batch run means silent success on the tail |
| Compounding split-point enumeration | Scales with word length × stem count | Partially — `MaxStemCount` is a caller setting, not grammar XML | Keep `MaxStemCount` minimal; anchor compounding subrule patterns tightly |

## Source grounding

Claims above are grounded in `src/SIL.Machine.Morphology.HermitCrab/` (engine) and
`src/SIL.Machine.Morphology.HermitCrab.Tool/` (batch CLI) as of the `conformance-framework` branch
tip at the time this file was written, plus the conformance fixtures under `conformance/edge-cases/`
and `conformance/languages/` on that same branch. The conformance framework (and the
`src/SIL.Machine.Morphology.HermitCrab.Conformance/` harness cited above) is on branch
`conformance-framework`, not yet merged to `master` — file/line references there may move once it
merges; check the live source if something looks stale.
