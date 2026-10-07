# Reuse feature-structure traversal state

**Status:** retained (`FeatureStruct.ValueEquals` and `Freeze`).
**Portable:** the allocation-avoidance principle is portable; the `[ThreadStatic]` pool is .NET-specific.

## Invariant

Equality and freezing need visited sets only after a recursive value is reached. A top-level
operation owns its traversal state until it returns. A nested top-level call cannot borrow that same state,
so it falls back to fresh collections.

## Implementation

The recursive APIs accept nullable state and allocate it only when traversal requires it. Ordinary top-level
calls borrow pre-sized sets and dictionaries from a per-thread pool, clear them in `finally`, and release the
pool. The pool has an `InUse` guard so reentrant calls retain the old independent-state semantics.

## Evidence

The heaviest Sena profile put feature-structure equality/freezing at 13.7% of wall time, with `HashSet`
resizing a major allocation source. After this change, `ValueEqualsImpl` and those resizes disappeared from
the top of the profile. This mechanism was not timed in isolation, so no standalone speedup is claimed.

## Correctness coverage and limits

`FeatureStructPooledVisitedSetsTests` covers reuse, clearing after success and failure, and reentrancy. Cyclic
and shared graphs continue down the general path. The complete conformance-inclusive HermitCrab run at the
final code tip passed 603 tests with one expected host-privilege skip.
