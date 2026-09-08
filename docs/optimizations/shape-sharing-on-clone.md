# Copy-on-write Shape sharing in `Word.Clone`

**Status:** retained (`Word.CloneForEngine`, `EnsureOwnShape`, `ResetShape`, `Shape.CreateEmptyLike`).
**Portable:** partly. The *observation* (three of four clone sites never use the copied shape) is
engine-independent; the mechanism (shared reference to an immutable structure) is how a Rust port would
naturally write it already.

## The observation

A CPU profile of the heaviest Sena word (60 s sampled, memoized path) put `Word` cloning at 36% of wall,
of which `Shape.CopyTo` was 28%: every clone deep-copied every `ShapeNode`, every node's feature
structure (`SimpleFeatureValue.CloneImpl` 22%), every annotation, and built a node-mapping dictionary.
The callers: `Word.ReplayOnto` 15% (memo replay; never mutates the shape), `Word.ExpandAlternatives` 9%
(immediately *replaces* `_shape`), `AnalysisAffixProcessAllomorphRuleSpec.ApplyRhs` 5% (`GenerateShape`
immediately `Clear()`s the copy), non-head clones 2.5%, `LexicalLookup` (the `RootAllomorph` setter
replaces `_shape`).

## The change

- Internal engine clone: if the source shape is frozen, share the reference and mark `_shapeShared`; otherwise
  deep-copy it. Public `Word.Clone()` always returns a deep, independently mutable shape.
- `EnsureOwnShape()`: clone once, on demand, before in-place mutation. Called in `AnalysisStratumRule` and
  `SynthesisStratumRule` before phonological rules are applied to a fresh clone (rewrite specs insert
  nodes and hold node references obtained by matching, so the un-share must precede the match).
- `ResetShape()`: replace with a new empty shape of the same configuration. Used by the four
  `GenerateShape`/rebuild sites (analysis affix, analysis compounding subrule, synthesis affix, synthesis
  compounding) instead of clone-then-clear.
- `ExpandAlternatives` marks the adopted shape as shared; the `RootAllomorph` setter and constructors mark
  their fresh clones as owned.

The safety net is that a shared shape is frozen: any mutation through a missed site throws
(`Shape.CheckFrozen`, frozen node feature structures; a missing guard on `Annotation.Range` was added).
During development the first full test run threw at exactly the two stratum sites, which is how they
were found; no other site surfaced across 122 HermitCrab tests, 464 fixture words and 45 Sena words.

## Measured

Profile after the change: `Word` clone 36% → 9% of wall, `Shape.CopyTo` 28.6% → 13% (the remainder is
the stratum's `EnsureOwnShape` and part copying inside `GenerateShape`), `ReplayOnto` 21.7% → 6.5%. Peak
working set on the heaviest word 11 GB → 6 GB. Wall-time attribution is unreliable on this machine (see
the round document); counts are unchanged by construction.

`WordShapeSharingTests` covers public-clone independence, copy-on-write, reset paths, and missed-mutation
guards. The final conformance-inclusive run passed 603 tests with one expected host-privilege skip.
