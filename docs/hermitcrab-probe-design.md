# HermitCrab probe design — enough to rebuild it

Companion to `hermitcrab-optimization-ledger.md`. That document records *what was tried and why it
failed*. This one records *how it was measured*, in enough detail to reconstruct the instrumentation
and the keys from scratch.

**Why this exists:** the seven probe branches that produced the ledger's numbers were deleted after
the round closed. Research scaffolding in two product assemblies is not worth maintaining — the
probes gate on `volatile bool` reads inside `Matcher.cs`, a hot inner loop in `SIL.Machine` used by
every consumer. So the code is gone and this is the design record. Anything here can be rebuilt in a
day; the expensive part was learning what to measure and which measurements lie.

---

## 1. Instrumentation architecture

Two probe classes, in two assemblies, with **two independent gates**.

`SynthesisProbe` in `SIL.Machine.Morphology.HermitCrab`, and `NodeCostProbe` in `SIL.Machine`. Each
has its own `internal static volatile bool Enabled`, default `false`. **Both must be set.** Setting
only the HermitCrab one makes every lower-layer bucket read exactly `0.00ms`, which looks like
"measured and negligible" rather than "never ran" — this cost a round (ledger method rule: a bucket
reporting exactly zero is broken until proven otherwise). Wire both from one place in the harness.

Everything is insertion-only: a `RecordDie(...)` or `Add*Ticks(...)` call placed immediately before
an existing `return` / `continue`, never a change to control flow. Where a site read
`else if (IsTracing) { trace }`, it becomes `else { if (IsTracing) { trace } Record(...) }` —
behaviour-preserving. Counters are `Interlocked`; timers are `Stopwatch.GetTimestamp()` deltas
accumulated with `Interlocked.Add`, with `Reset*` methods so a harness reads per-word deltas.

### The wall-time split

**Eight mutually exclusive top-level slices that sum to wall**, measured around
`Morpher.ParseWord`:

`lookup` (brackets `LexicalLookup`, materialised with `.ToList()` so the bracket covers only the
lookup itself) · `synCascade` · `synBattery` · `synForward` · `synExpand` (brackets
`Word.ExpandAlternatives()` in `SynthesizeSequential`) · `anTotal` (brackets the whole
`_analysisRule.Apply(input)` call) · `unaccounted` (defined as the remainder, never tracked).

`synForward` is recorded **net of** `synCascade` and `synBattery`: capture both counters before the
`_synthesisRule.Apply` call and subtract their deltas after. This matters enormously and caused a
25x published error — the work a synthesis fold memo would share runs *inside* the cascade and
battery brackets, so the share to divide by is `synCascade + synBattery + synForward`, not
`synForward` alone. Template slot rules compile to the same `SynthesisAffixProcessRule` classes via
`RuleBatch` (`SynthesisAffixTemplateRule` ctor), which is why they land in `synBattery`.

Nested *inside* `anTotal`, as a breakdown rather than additional slices: `anCascade` (the
`_mrulesRule.Apply` call in `AnalysisStratumRule.ApplyMorphologicalRules`), `anBattery`
(`ApplyTemplateBattery`), `anPhono` (`_prulesRule.Apply`).

**Always print the remainder as its own column.** It caught the misattributed split, the dead probe
gate, and would have caught four of five retractions at the point of measurement.

### The per-node decomposition

Inside `anCascade` and `anBattery`, sub-buckets: `match` · `feature` · `clone` · `key` · `memo` ·
`trail` · `equality` · explicit remainder.

`match` and `feature` come from the `SIL.Machine` layer. To avoid double counting, the HermitCrab
side reads `NodeCostProbe.MatcherTicks` / `FeatureTicks` **before and after** the cascade call and
records the delta — so the lower-layer buckets are slices *inside* the HermitCrab bucket, not
alongside it. Matcher self-time has nested work subtracted at the source (inside `Matcher.cs`);
feature work needs a re-entrancy depth guard or recursive `Unify` calls double-count.

Measured outcome, so a rebuild can sanity-check itself: on Amharic `anCascade`, clone **46.7%**,
remainder **37.4%**, match 12.5%, feature 3.4%, key **0.0%**, and memo / trail / equality each
**<0.2%**.

### Censuses

Rule attempts, split by which guard rejected them (`guardSelector`,
`guardApplicationCount`, `guardUnifiability`, `reachedMatching`); allomorph pattern-match
attempts vs matches; clone created vs discarded vs kept.

Die points, as an enum with a counter array **and a parallel tick array** — timing each individual
guard check, not the enclosing `Apply()`. Count and cost diverge: `RuleNotApplicableOrPatternMismatch`
was **100.0% of events** and about **0.2% of time**, because each rejection is one list index plus one
reference compare (~29 ns). Never report a die-point histogram by count alone.

Die points, classified for the abstract-replay question — **shape-free**: synthesis-side
`MaxApplicationCount`, syntactic-feature unification, MPR features, realizational
subsumption/blocking, co-occurrence and obligatory features in `IsWordValid`. **Shape-dependent**:
allomorph pattern match / rule-not-applicable, allomorph environments, final `IsMatch`.
`LexicalLookupMiss` is neither — it precedes any synthesis step.

---

## 2. The keys

### `AnalysisStateKey` (pre-existing)

`(Shape, Stratum, SyntacticFeatureStruct, RealizationalFeatureStruct, NonHeadCount, per-rule
un-application count multiset)`. Order-independent by design — no analysis-side rule reads trail
order, which is the redundancy it collapses. The multiset is XOR-combined, not `*31`-rolled, so it
hashes commutatively.

**Freeze-on-read in the constructor is mandatory.** `AnalysisAffixTemplateRule.Apply` reassigns
`SyntacticFeatureStruct` to a fresh unfrozen clone *after* the owning `Word` is frozen, and that
setter has no `CheckFrozen()` guard.

### The narrowed variant (row 4)

Filter the count multiset to rules that are **not** provably shrinking. Two requirements learned the
hard way: build the retain-map **once per grammar**, not per key (key construction is on the hottest
path); and filter during hashing and equality rather than by materialising a filtered dictionary, so
the narrow key allocates no more than the full one. **Absent from the map means retain** — a rule the
classifier walk does not cover must be counted, never silently dropped.

A `retainedCount` field is needed so equality can compare sizes without enumerating.

### `SynthesisStateKey` (row 5) — and the trap

Everything a synthesis step reads: shape+annotations, syntactic FS, realizational FS, MPR set, root
allomorph, disjunctive allomorph indices, applied-rule counts, `IsPartial`,
`IsLastAppliedRuleFinal`, stratum, **and the ordered remaining trail** —
`Word.MorphologicalRuleTrail` sliced to `[0, PendingTrailPosition]`.

**The trail content is the whole game.** A key with `PendingTrailPosition` (an integer index) and no
trail content is fine for *measuring* and catastrophic for *skipping work*: two candidates at the
same index with different pending sequences compare equal, and merging them discards one
derivation's continuation. Every large apparent redundancy in this engine was measured with such a
key, and each collapsed when the content was added — 9,774x → 15–40%, 61-of-395,026 → 351,414-of-395,026,
3.22x/8.10x → `hits = 0`.

**`Word.ValueEquals` is not a starting point.** It omits `_syntacticFS`, MPR features and the
disjunctive allomorph indices, all of which synthesis reads. Inheriting from it produces a silently
inflated ratio.

Two more properties a real memo needs: stored results are **sets**, not values (realizational rules
are trail-exempt — `SynthesisRealizationalAffixProcessRule` has no `IsMorphologicalRuleApplicable`
gate — and several allomorphs of one rule can legitimately match one input before the disjunctive
break); and stored outputs embed the producing candidate's trail, so replay must **re-anchor**, the
mirror of `Word.ReplayOnto`.

### `RuleLengthClassifier` (row 4's static half)

Verdicts: `Shrinking` / `NonShrinking` / `Unknown`, where `Unknown` is treated identically to
`NonShrinking` by the key and kept distinct only so a census can tell "this grammar has zero
morphemes" from "this grammar has constructs we cannot analyse".

An `AffixProcessAllomorph` shrinks iff **every** allomorph (a) inserts at least one *segment*
(`InsertSegments` counting only `HCFeatureSystem.Segment` nodes — a boundary-only insertion is
conservatively NonShrinking, since `InsertSegments.GenerateAnalysisLhs` omits boundaries from the
analysis pattern; `InsertSimpleContext` counts as 1), (b) has **every Lhs part captured** by a Rhs
`CopyFromInput` or `ModifyFromInput`, and (c) copies no part twice.

The direction trap, which is easy to invert: on the analysis side rules are *un*-applied, so a rule
that inserts on synthesis **removes** on analysis, and a rule that truncates on synthesis
**untruncates** — an Lhs part with no Rhs copy makes the un-applied word **grow**. The authority is
`AnalysisMorphologicalTransform.GenerateShape`, which walks the Lhs parts and either copies the
captured span or calls `Untruncate` to regenerate the part from its pattern. Reduplication (a part
copied twice) and `CompoundingRule` are `Unknown`; conservatism only costs pruning opportunity.

Measured census, for a rebuild to check against: Sena 19 of 27 rules shrinking, Indonesian 10 of 15,
Amharic 31 of 36.

---

## 3. Harness design

`[TestFixture]`, `[Explicit]`, environment-driven, writing only `TestContext` lines and no files —
this repo never commits real grammars, and derived corpus data must not leak into a committed path.
**Never print rule, stratum, morpheme or lexical-entry names, or word lists.** Counts, ratios and
timings only. Conformance fixture IDs are committed and safe.

Env: `HC_MEMO_GRAMMAR`, `HC_MEMO_WORDS`, `HC_MEMO_MAX_WORDS`, `HC_PROBE_WORDS` (explicit
comma-separated list), `HC_PROBE_FIXTURES_ROOT`, `HC_MEMO_TIMEOUT_MS`.

Breadth comes from `SIL.Machine.Morphology.HermitCrab.Conformance.Fixture.DiscoverAll(root)` over
`conformance/{languages,edge-cases}` — 33 grammars, each `grammar.xml` + `words.yaml` with
hand-derived expectations. Depth comes from the three real corpora. **Amharic: never exceed 28
words** — word 29 (`ሌባዎቹ`) is pathological and burns hours.

Run the memoized path: `new Morpher(traceManager, language, maxDegreeOfParallelism: 1)`. Tracing
disables the memo, so a traced run measures nothing.

### A/B methodology, all of it earned by being wrong first

Parity is the gate and speed is only reported. Acceptance is **analysis-set equality on canonical
morpheme-signature sets** (`MorpherTests.WordAnalysisSignature`, sorted) — never object or byte
equality, because a replayed `Word` is legitimately not field-for-field identical.

Timing: **discard a warm-up rep per arm, then take min of N interleaved samples, and print the
off-arm spread beside every speedup.** Refuse any speedup smaller than its own spread. Three
failures forced each part of this — a first A/B read 1.53x that was entirely JIT; another read
2.385x with a 210.9% spread on one arm that was 1.015x serial; and adding one arm's warm-up while
discarding the other's manufactured a uniform ~2.4x across every fixture including ones with zero
shareable steps.

**Never dispatch timing-sensitive probes concurrently.** Three at once put four testhosts on the
machine and invalidated three rows' bucket percentages. Check `tasklist | grep -c testhost` before
any timing run and report the count alongside the numbers. Counts survive contention; shares and
speedups do not.

**Never aggregate the test logger's console output.** It prints every line twice, the second copy
space-prefixed, so a `^\s*` grep doubles every total — this published a 2x error.

Sub-50 ms fixtures cannot carry a timing claim: one 10 ms fixture's share moved 1.3% → 14.5%
between two runs of identical code. Flag them (`reliable = wallMs >= 50`) rather than filtering them
out silently.

### One concurrency hazard worth knowing

A per-word timeout wrapper built on `Task` has no cooperative cancellation, so an abandoned word's
task keeps mutating engine state after the harness moves on. Harmless with a per-word
`AnalysisScope`; under a **shared** scope it collided with the next word on the same
`Memo`/`InProgress` collections and produced `IndexOutOfRangeException` inside
`MemoizedCombinationRuleCascade`. A shared-arm timeout must abort the run, not continue. Also note
`Task.Result` throws `AggregateException`, not the bare exception type.

---

## 4. Two durable engine facts, restated here so they survive independently

**`Word.ReplayOnto` does not splice `_mrulesUnapplied`.** It splices `_mruleApps` and `_nonHeadApps`
only. This is safe *solely* because `AnalysisStateKey` includes the count multiset, which guarantees
a memo hit has identical arrival and stored-arrival counts. **Narrowing that key breaks correctness
with no test failure.** Fix, if the key ever changes: store arrival counts on `MemoEntry` and compute
`stored - storedArrival + query` — a no-op under today's key, which is also how to test it.

**`AnalysisScope.MaxMemoEntries` is a per-scope cap with no eviction.** Sharing one scope across a
corpus therefore makes words compete for a fixed budget: two heavy Sena words exhausted it at index
74 and 76, and every later word lost the within-word memoization a fresh scope would have given it.
Cross-word hits went *negative* and the corpus ran 28–32% slower. A shared bounded cache without
eviction is not a smaller version of the optimization.
