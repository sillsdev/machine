# HermitCrab Review

*Review HermitCrab morphology changes for analysis equivalence, memoization-key
completeness, retained-memory bounds, parallelism, and hot-path cost.*

Governs `src/SIL.Machine.Morphology.HermitCrab/**/*.cs`.

- Treat analysis output as the primary contract. A faster parse, more memo hits, or a
  successful build does not prove equivalent analyses.
- When changing analysis-side rules or state, re-audit every field the rule reads
  against `AnalysisStateKey`. Check freezing, cached hashes, mutable dictionaries,
  equality, rule counts, non-head counts, feature structures, and stratum identity.
- Memoized results must represent fully expanded subtrees. Check replay prefixes,
  deduplication, empty/nogood entries, in-flight recursion, and the separation between
  sequential and parallel scopes.
- Do not weaken an existing memo or retained-word bound without measured evidence
  and a test. Read the current limits from the code.
- Inspect allocations and retained object lifetimes only in changed inner loops. If the
  change claims a performance improvement, require a reproducible benchmark or measured
  artifact in addition to semantic regression tests.
- Exercise sequential and parallel behavior where the changed path supports both, and
  test cancellation/disposal if a boundary is asynchronous.
