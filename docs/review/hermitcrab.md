# HermitCrab Review

*Review HermitCrab morphology changes for analysis equivalence, state-sensitive
merging, parallelism, and hot-path cost.*

Governs `src/SIL.Machine.Morphology.HermitCrab/**/*.cs`.

- Treat analysis output as the primary contract. A faster parse or a successful build
  does not prove equivalent analyses.
- When changing analysis-side rules or state, check every field a rule reads and
  whether equivalent analyses can be merged safely. Check rule counts, non-head
  counts, feature structures, and stratum identity.
- Inspect allocations and retained object lifetimes only in changed inner loops. If the
  change claims a performance improvement, require a reproducible benchmark or measured
  artifact in addition to semantic regression tests.
- Exercise sequential and parallel behavior where the changed path supports both, and
  test cancellation/disposal if a boundary is asynchronous.
