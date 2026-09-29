# Reuse or skip the feature-structure clone map

**Status:** retained (`FeatureStruct.Clone`).
**Portable:** yes; the specific pool is .NET-oriented, while the tree proof is general.

## Invariant

A frozen structure marked as a flat tree has no aliases or cycles to preserve below its root. Its top-level
clone therefore cannot encounter the same value twice, so the old-to-new copies map has no semantic work to
do. Recursive cloning inside another graph must still use the caller's map even when the child itself looks
tree-shaped, because the parent graph may refer to that child elsewhere.

## Implementation

A top-level clone skips the map for a proven frozen flat tree. General clones borrow a per-thread map, clear
it in `finally`, and fall back to an independent map for reentrant calls. The internal recursive constructor
always honors the caller-provided map.

## Evidence

This removes dictionary creation and lookup from the common clone case. It travelled with the broader clone
allocation work and was not timed independently, so the record deliberately makes no isolated speed claim.

## Correctness coverage and limits

`FeatureStructCloneTests` covers alias preservation, cycles, reentrant cloning, and the distinction between a
top-level tree and a tree embedded in a shared graph. The complete conformance-inclusive run passed 603 tests
with one expected host-privilege skip.
