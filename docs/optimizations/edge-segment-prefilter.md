# Edge-segment prefilter for analysis affix rules

**Status:** retained (`AnalysisAffixProcessRule`; internal same-binary toggle
`Morpher.EdgePrefilterEnabled`). **Portable:** yes; it is a property of the pattern semantics,
not of the C# engine.

## The observation

Every analysis affix pattern is compiled with `AnchoredToStart = true`, `AnchoredToEnd = true`,
`MatchingMethod = Unification`, and a filter that shows the matcher only Segment-type annotations. The
pattern is built from the allomorph's Lhs/Rhs: copied stem parts become groups with quantifiers, and each
inserted segment becomes a single `Constraint` node carrying that segment's feature structure
(`InsertSegments.GenerateAnalysisLhs`). A `Constraint` compiles to one mandatory arc with no epsilon
alternative (`Constraint.GenerateNfa`).

So when the pattern's first top-level node is a `Constraint`, the traversal's first step must unify it
with the shape's first segment node, or the traversal fails; likewise the last `Constraint` and the last
segment node. The only way an annotation can be skipped is when it is marked `Optional`.

On the heaviest Sena word, 3,564,736 pattern-match attempts produced 170,863 matches (95% failed), and
each failed attempt still ran a full nondeterministic traversal: 4,017,574 traversal instances,
9,111,459 arc checks.

## The change

At compile time, `AnalysisAffixProcessAllomorphRuleSpec` records `LeftEdgeConstraint` /
`RightEdgeConstraint`: the feature structure of the pattern's first / last child when that child is a
bare `Constraint` (null when it is a group, quantifier or alternation). At run time, before each
allomorph's `MultiplePatternRule.Apply`, `AnalysisAffixProcessRule.Apply` finds the shape's first and
last Segment-type nodes (skipping boundaries exactly as the matcher's filter does) and rejects the
allomorph if `edgeNode.FeatureStruct.IsUnifiable(edgeConstraint)` fails, using the same unification call
and `UseDefaults` setting the matcher itself uses. An `Optional` edge node disables the check on that
side. When tracing, the same `MorphologicalRuleNotUnapplied` event is emitted that the matcher's failure
would have produced, so traces are unchanged.

## Measured (deterministic counts, sequential path, one parse per word, same binary)

| 45 Sena words | prefilter off | prefilter on |
| --- | --- | --- |
| FST traversals | 12,376,416 | 1,617,662 |
| FST arc checks | 34,170,777 | 15,583,242 |
| traversal instances used | 15,642,175 | 2,429,643 |
| pattern-match attempts (counted before the check) | 11,979,916 | 11,979,916 |

Parity: identical analysis signature sets on all 45 Sena words and on all 464 conformance-fixture words
(33 grammars). Fixture traversals fell 21% (31,278 to 24,741); their patterns fail less often.

`AnalysisEdgePrefilterTests` covers left/right constraints, boundary skipping, optional nodes, and the
disabled path. The broader HermitCrab and conformance suites cover integration behavior; the final
conformance-inclusive run passed 603 tests with one expected host-privilege skip.

## Why the ledger's estimate was wrong

The ledger (row 22) capped a prefilter at ~10% using the matcher's *self-time* share. The traversal's cost
is mostly booked elsewhere (instance and register allocation, feature unification, GC), so self-time
understated it by a large factor. By counts, the prefilter removes 87% of traversals outright.

## Limits

After the change, ~44% of the remaining traversals succeed, so a second-segment check has little left to
reject. The rule loop still runs ~110 attempts per cascade state; depending on which safe edge constraints
exist and whether the first rejects, an attempt performs zero, one, or two prefilter unifications before any
remaining match. Indexing allomorphs by edge segment could avoid both checks and matching work, but its
headroom was not established and it was not attempted.
