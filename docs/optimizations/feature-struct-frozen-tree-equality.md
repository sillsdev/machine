# Allocation-free equality for frozen flat feature structures

**Status:** retained (`FeatureStruct.ValueEquals`).
**Portable:** yes; the proof depends on immutability and tree shape, not the CLR.

## Invariant

During `Freeze`, a feature structure is marked as a flat tree only when every reachable value is a
`SimpleFeatureValue`, no value instance repeats, and no nested `FeatureStruct` is present. Once frozen, that
fact cannot change. Comparing two such structures can never trigger the general algorithm's reentrancy or
cycle guard, so a feature-count/key/value comparison is equivalent to the general visited-pair walk.

## Implementation

`ValueEquals` uses the direct leaf comparison only when both operands are frozen and carry the tree proof.
Every other shape—including shared leaves, nested structures, and cycles—uses the general algorithm. A
test-only slow-path entry point lets randomized tests compare both implementations on the same inputs.

## Evidence

The optimization removes all visited-set work from the dominant frozen, flat case. It was observed in the
same profile that motivated traversal-state reuse, but was not isolated from the other feature-structure
changes; no standalone wall-time ratio is claimed.

## Correctness coverage and limits

`FeatureStructTreeFastPathTests` cross-checks randomized flat structures against the slow path and exercises
false comparisons and structures that must not qualify. `FeatureStructValueEqualsFreezeTests` covers shared
and cyclic graphs. The full conformance-inclusive run passed 603 tests with one expected host skip.
