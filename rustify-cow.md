# RUSTIFY on top of memoization — does the array/COW rearchitecture help?

**Branch:** `feature/memoization-plus-cow`, stacked on `feature/memoization` (PR #456), which
is stacked on master. This PR's own diff is exactly one commit: the previously-reviewed,
already-squashed `RUSTIFY: allocation- and CPU-optimized HermitCrab parsing` (originally
`ea72cd79`, PR #446), cherry-picked onto `feature/memoization` and merge-resolved.

**Question this answers:** `memoization.md` §5 flagged an open hypothesis — master lacks
RUSTIFY's copy-on-write `Shape`/`ShapeNode` (flat, array-backed, COW-cloned) that the original
prototype had, and `Word.ReplayOnto`'s plain deep-clone graft was suspected to cost more without
it. One heavy word (H1) still hit the ≥3x target anyway; a second heavy word (H2) did not
complete within a 400s memo-on budget at all, which was the first concrete data point the
deep-clone tax might be real. This PR tests that directly: layer RUSTIFY's array/COW rearchitecture
on top of memoization and re-measure the same two words, same methodology, same grammar.

## What's actually in this PR

RUSTIFY's own summary (from its commit message, unchanged by this port):
- Flat/COW `Shape` and `ShapeNode` backing (parallel int-linked arrays instead of an
  `OrderedBidirList`; `ShapeNode` becomes an `(Owner, Index)` handle).
- Copy-on-write `Shape` cloning: a clone of a frozen shape shares the source's backing until
  first mutation.
- `FeatureStruct` bit-packed `ulong` flat-unify fast path for the common simple/no-variable
  case, falling back to the original engine otherwise.
- int-offset FST traversal throughout (every HermitCrab rule-spec file migrated from
  `ShapeNode`-offset to `int`-offset pattern matching).
- Cheap `GetHashCode` overrides on several hot dictionary/hashset key paths; `StringComparer.Ordinal`
  on hot sorts; a shared per-thread `Random`; a filtered-annotation-view cache on frozen
  `AnnotationList`s.
- `SyntacticFeatureStruct` mutate-after-freeze correctness hardening.

This is **general allocation/CPU work, not FST-specific** — nothing here compiles a grammar to
an FST or changes analysis semantics. It was already independently reviewed as PR #446 (see
memory: byte-identical to master on the full regression suite plus per-word signature diffs on
Sena and Indonesian) before this port.

**Deliberately NOT included:** the FST inverse-chain analyzer, `GrammarFstAdvisor`, and the
~5,000 lines of FST planning/spike docs that a separate exploratory branch (`fst-advisor`) had
bundled together with a rebased copy of this same RUSTIFY work. Those are a different
optimization strategy (compile-to-FST vs. memoize-the-search) with their own, separately
confirmed correctness gap on pathological words — out of scope here. This PR is the
allocation/data-structure rearchitecture alone, isolated from that bundling, specifically so
it can be measured as its own independent variable against memoization.

## Merge notes (mechanical, not semantic)

RUSTIFY (based on an older master commit) and memoization (based on current master) both touch
`Word.cs`, `Morpher.cs`, `AnalysisStratumRule.cs`, and `MorpherTests.cs`. All four resolved
cleanly by inspection — the conflicts were positional (both branches independently added new
constructor logic / new tests near the same anchor lines), not competing implementations of the
same behavior:
- `Morpher`'s `maxDegreeOfParallelism` ctor: kept RUSTIFY's more general two-overload shape
  (defaults to `Environment.ProcessorCount`, throttles the parallel cascade to any requested
  degree) while preserving memoization's `== 1` → memo-eligible trigger. These compose cleanly:
  memoization only ever cared about the `1` case.
- `AnalysisStratumRule`'s cascade selection: kept memoization's `MemoizedCombinationRuleCascade`
  for `== 1`, adopted RUSTIFY's `MaxDegreeOfParallelism` throttle on the parallel cascade for
  other values (a real improvement memoization-alone never had — it left the parallel path
  running at an unthrottled default regardless of the requested degree).
- `ApplyTemplates`'s outer `.Distinct(...)` call: RUSTIFY had already removed this (both here and
  on the mrule-cascade call site) as a verified-redundant no-op — `CombinationRuleCascade`/`RuleBatch`
  already dedupe internally via their own `HashSet<TData>(comparer)`. Confirmed by reading
  `RuleCascade`/`RuleBatch`'s own `Apply` implementations, not just trusting the commit message.
  Adopted RUSTIFY's removal.
- `Word`'s clone constructor: adopted RUSTIFY's lazy-null `_disjunctiveAllomorphIndices` allocation
  (only allocate when the source has entries) alongside memoization's `AnalysisScope = word.AnalysisScope`
  copy — independent fields, no interaction.
- The engine-wide `ShapeNode` → `int` offset-type change (RUSTIFY) required updating memoization's
  own new files (`MemoizedCombinationRuleCascade : RuleCascade<Word, ShapeNode>` →
  `RuleCascade<Word, int>`, and the corresponding `IRule<Word, ShapeNode>` references in its test
  file) to match. `AnalysisStateKey.cs`, `AnalysisScope.cs`, and `Word.ReplayOnto` needed no
  changes — they operate on `Shape` (whose public `Freeze`/`GetFrozenHashCode`/`ValueEquals` API
  RUSTIFY preserves) and on `_mruleApps`/`_nonHeadApps` (untouched by the Shape rearchitecture),
  never on the offset type directly.
- `MorpherTests.cs`: both branches added independent new tests at the same anchor point (RUSTIFY:
  `AnalyzeWord_SingleThreaded_MatchesParallel`, `AnalyzeWord_ConcurrentRepeatedParsing_IsDeterministic`;
  memoization: 4 tests + `WordAnalysisSignature`). Kept all of them; no actual overlap in behavior
  tested.

Gate: 80/80 HermitCrab tests pass (78 from memoization + 2 from RUSTIFY), full `Machine.sln`
test suite green.

## Measured result: analysis-set identical, and faster on both sides

Same methodology as `memoization.md` §5: canonical analysis-set signature comparison
(never byte-identical object comparison), same two heavy words (H1, H2 — real corpus words,
never named per the standing grammar-privacy constraint), same local uncommitted Sena grammar.

| Word | memo-on (seq+memo+RUSTIFY) | memo-off (parallel+RUSTIFY) | Ratio | Divergences | vs. memoization-alone |
|---|---|---|---|---|---|
| H1 | **9.17s** | 90.0s | **9.81x** | 0 | was 22.1s / 136.1s / 6.2x — both sides faster, ratio improved |
| H2 | **44.8s** | 263.8s | **5.89x** | 0 (both empty) | was >400s / >400s (never completed) — now completes cleanly |

**H1** is the strong, unambiguous result: a real analysis (2 valid parses), confirmed
analysis-set identical, and RUSTIFY makes *both* the memo path and the parallel-default path
faster — the memo path more so (22.1s → 9.17s, 2.4x) than the parallel path (136.1s → 90.0s,
1.51x), consistent with `ReplayOnto`'s clone being exactly the kind of operation COW should help
most.

**H2** is the headline: the word that memoization alone could not get through even a 400s
budget now completes in 44.8s (memo-on) — this is the concrete answer to the open question
`memoization.md` raised. One honest caveat: `words with no parse on both sides: 1` — H2 resolves
to *zero* valid analyses on both memo-on and memo-off in this grammar (not a positive multi-analysis
match like H1). The soundness confirmation here is "both sides agree on empty," not "both sides
agree on the same non-trivial analysis set" — still a real, non-vacuous check (434,628 nogood
hits recorded means the search space explored before concluding "no parse" is exactly the
pathologically large one this word is known for), but weaker than H1's positive match. Worth
re-confirming on a heavy word that *does* have a known-positive analysis if one is found.

**A first anomalous run is worth recording, not hiding:** the first H1 attempt at a 240s budget
timed out — alarming on its face, since 9.17s + 90.0s = 99.2s is comfortably under 240s. Note
what "timed out" means precisely here: the harness's per-word timeout wraps both the memo-on
and memo-off calls in one try block, so *no per-word timing* was captured for this word either
way, but the `DiagMemoHits`/`DiagNogoodHits`/etc. counters are separate `static` fields printed
after the loop regardless — whatever the (still-running, since the harness's `RunWithTimeout`
doesn't cancel on timeout) background computation had reached by read-time is what gets
reported. That read showed the run's final tally (29,736 / 88,426 / 37,512 / 0) exactly matching
the same word's later, completed 600s-budget run. This is suggestive, not conclusive, evidence
the failed run's computation had actually finished by the time it was read rather than being
genuinely stuck partway through: a deterministic search reaching the exact correct final count
by coincidence mid-computation is possible in principle but a priori unlikely for a large,
non-round total. Combined with a 600s re-run completing cleanly at 9.17s/90.0s (nowhere near
600s), the more probable explanation is transient machine load inflating that one run's wall
time past 240s (this session had already run many consecutive heavy CPU benchmarks
immediately beforehand), not a real regression from the merge — but this is a plausibility
judgment, not a proof, and is exactly why the re-run (not the counter match alone) is what this
PR's numbers are actually based on.

## Typical (non-template) grammars: no change to the existing tradeoff

Re-ran the Indonesian aggregate (121 words, same as `memoization.md`'s own measurement):
0 divergences, memo-on total 2,425.7ms vs memo-off total 1,758.4ms (0.72x, ~38% slower) —
within the same noise band memoization-alone showed on this grammar (0.68x-0.80x across 3 reps).
RUSTIFY doesn't measurably change the typical-word tradeoff in either direction here; the
existing honest caveat in `memoization.md` §6 (sequential+memo loses to parallel-default on
typical words, mostly from the lost thread) stands unchanged.

## Bottom line

Stacking this rearchitecture on top of memoization is a real, verified improvement, not a
"kind of, but not really": both measured heavy words get faster, and the one word memoization
alone couldn't complete at all now does, at a **5.89x** ratio, with soundness holding
(0 divergences on both). The cost is a much larger diff than memoization alone (110 files vs 11)
touching the engine's core data representation — reviewed once already as PR #446, re-verified
here against the current codebase plus memoization's new code paths.
