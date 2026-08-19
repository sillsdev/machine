# The coverage pipeline: inventory, evidence, proof, completeness

The claim this pipeline has to support is not "we ran a lot of tests". It is: **here is the inventory of
what must be covered, and here, per item, is the example and the counter-example.** Every part below
exists to make that claim mechanically checkable rather than asserted.

## Contract

For every item in the inventory, exactly one of these must hold, and the gate recomputes which:

1. **Evidence** — a counter-example: the item is neutralized in a copy of a fixture's grammar and a
   recorded outcome changes.
2. **Proof** — an impossibility claim of a named kind, re-verified at gate time.

Nothing else passes. An item with neither fails the build. An item with both fails the build, because a
proof that outlives its own evidence is stale by definition.

Three properties the contract depends on, each already paid for once:

- **Neutralization is exhaustive, not arbitrary.** An enum value is neutralized against EVERY declared
  sibling, not one. Trying a single arbitrary sibling silently reports "no difference" when the sibling
  chosen happens to be a synonym, which is a statement about our tooling wearing the costume of a
  statement about the engine.
- **A counter-example names its word.** Evidence proven with a probe word that is not in the fixture's
  `words.yaml` is not evidence, because the sweep never parses it.
- **"I could not look" never reads as "nothing there."** A timeout is not a pass, and an unobservable
  mutation is not a proof.

## Data model

```
CoverageItem                          the inventory: what we have committed to cover
  id            stable, content-derived
  kind          Surface | Ordering | Interaction
  origin        dtd-element | dtd-enum | adjacent-transposition | rule-pair
  fixture       where it is expected to be evidenced

Evidence                              produced mechanically, never hand-written
  itemId
  fixture             which fixture supplied the evidence
  exampleWord         the word, present in that fixture's words.yaml
  exampleOutcome      its outcome with the item intact
  counterexampleKind  Word | LoadFailure | None
  counterexampleOutcome
  mutation            exactly what changed, including WHICH sibling
  verdict             Evidenced | RequiredByDtd | RequiredByLoader | EvidencedJointly
                      | Unobservable | Timeout

Proof                                 only where evidence is impossible
  itemId
  kind                dtd-default | no-consumer | not-in-signature | blocked-by-defect
                      | disjoint-domains | unordered-invariant | inactive-member
                      | pos-disjoint | template-masked | never-fires | feature-value-disjoint
  check               what the gate RECOMPUTES; never free prose
```

The last seven kinds above are Ordering-only, recomputed fresh from the checked-in grammar on every gate
run rather than read from a file: `disjoint-domains` (an earlier rule's effect cannot reach a later rule's
sensitive set), `unordered-invariant` (the pair's own Stratum compiles to a cascade that tries every rule
regardless of position), `inactive-member` (one member of the pair either carries `isActive="no"` itself,
so its list position is never read, or -- for an AffixTemplateSlots pair -- is an active Slot whose
referenced `morphologicalRules` all fail to resolve to an active rule, including a Slot with no such
attribute at all, so the Slot contributes no morpheme wherever it sits), `pos-disjoint` (the two rules'
`requiredPartsOfSpeech` cannot both be satisfied by one root), `template-masked` (an AffixTemplateSlots
pair whose owning Stratum is unordered AND every rule referenced by every Slot of that template is also in
the Stratum's own `morphologicalRules` list, so the cascade already reproduces everything the template can
produce and slot order is invariant), `never-fires` (a `PhonologicalRule`'s shared `PhoneticInput`, or
every one of its active subrules' `LeftEnvironment`/`RightEnvironment`, names a natural class that resolves
to zero active segments -- counting only active `FeatureValue`/`SegmentDefinition` declarations, mirroring
the loader -- so the rule can never fire and its list position is a no-op regardless of which sibling it
sits next to), and `feature-value-disjoint` (two `PhonologicalRule`s whose active subrules are gated by
disjoint `Environment` natural classes, where neither rule's output segment is a member of the other's
environment class or its `PhoneticInput` class, so at most one can ever match a given site and neither can
create a new site for the other -- the case `disjoint-domains` alone cannot certify because the two rules
may share the same input class, which looks like an overlap until the environments are shown mutually
exclusive).

`Kind` stays on `CoverageItem` and off `Evidence` — an item's kind is a property of the item, not of one
piece of evidence for it — so a ledger row is a join of the two.

**A proof's `check` must be recomputed, and the recomputation must be about the GRAMMAR, not about the
fixture's words.** The distinction is the whole contract. "No word in this fixture triggers the rule" is
the GAP condition — it cannot distinguish independence from test words too weak to tell — and writing it
in the `check` column converts an untested item into a proven one. A proof says two rules cannot interact
in any grammar of this shape; if adding a word could falsify it, it was never a proof.

`label-symmetry` was removed rather than kept. Its only use claimed that alpha-variable names are
interchangeable labels; that was false, because alpha variables unify across every `VariableFeature` a
rule declares, so renaming one to collide with another reverses which words parse. The check was weak in a
specific and repeatable way: it never required the two values to CO-OCCUR in a grammar before believing
they were interchangeable.

`counterexampleKind` is the field that stops a weaker result from hiding inside a strong headline.
`LoadFailure` says the mutant would not load — real evidence that the item is structurally required, but
NOT a word-level contrast, and the two must never be summed into one number without saying so.

## Generators

The inventory is generated, never hand-maintained; a hand-edited inventory is one that shrinks to fit
whatever happens to pass.

| Generator | Source | Items |
|---|---|---|
| Surface | the DTD's elements and enumerated attribute values | 194 |
| Ordering | adjacent transpositions of each ordered list | 146 |
| Interaction | rule pairs whose domains structurally overlap | 58 of 1,305 (see below) |

That cell read "≤ 627 pairs" until 2026-08-14. The figure could not be reproduced from any
checked-in artifact and no committed computation yields it; it appears to be a design-time
upper-bound estimate, in the same family as the 1,465 and 1,342 estimates corrected elsewhere.
The measured value is the ledger's `Overlaps` count: 58 of 1,305 rows, against 30 `Disjoint` and
1,217 `Undetermined`.

**This "Interaction" row is `conformance/rule-interaction-pairs.tsv`'s generator, and it is a
per-grammar pruning device, not a coverage denominator** — `docs/coverage-strategy.md` is explicit
that the row count grows with the fixture set (1,305 rows currently, 1,217 `Undetermined` by
construction) and must never be cited as bounding the interaction space. The actual mechanical
denominators above the surface/ordering layers are `docs/coverage-strategy.md`'s integration/edge
layer (`conformance/interface-inventory.tsv`, DTD-derived, 60 interfaces) and integration/chain layer
(in progress), both sized from the DTD and engine rather than from how many pairs the current corpus
happens to contain.

**Ordering is n−1 per list, not n! and not C(n,2).** Adjacent transpositions generate the symmetric group,
so pinning every adjacent swap pins the total order. The two 16-rule lists in the corpus cost 15 items
each rather than 2×10¹³ permutations. A non-adjacent swap decomposes into adjacent ones and buys nothing.

**An unobservable swap must prove independence, not pass by silence.** That is what the
`disjoint-domains` proof kind is for, and it has to cover feeding and bleeding, not only the input
pattern a rule matches: the earlier rule's EFFECT (its output segments, plus its own input segments,
since consuming or altering a segment can destroy an environment as surely as producing one can create
it) and the later rule's SENSITIVE set (its input segments, plus every segment reachable from its
`Environment`/`LeftEnvironment`/`RightEnvironment` templates) must not intersect, recomputed from the
grammar at gate time. An earlier check that compared only output against input missed exactly this:
`mpr-gated-exception`'s `prNasalAssimAlveolar`/`prObstruentDeletion` pair was certified `disjoint-domains`
because the outputs and inputs alone never intersect, but `prObstruentDeletion` only fires inside a
`LeftEnvironment` of nasal segments, and `prNasalAssimAlveolar`'s output IS nasal -- classic feeding, and
empirically confirmed (swapping the pair changes `menulik` from a real parse to no parse at all).
Otherwise "swapping these changed nothing" is indistinguishable from "our words are too weak to tell".

**Interaction is the 3-leg necessity check** already built for joint mutation — disable A, disable B,
disable both — because interaction is exactly `effect(A∧B) ≠ effect(A) ∘ effect(B)`. Pairs whose domains
cannot overlap are pruned by the same static check that backs `disjoint-domains`.

## Completeness

One gate, recomputed from the fixtures on every run:

- every generated item resolves to Evidence-with-a-counter-example or a Proof that re-verifies;
- no Proof names an item that is now evidenced, or an item no longer in the inventory;
- the checked-in ledger matches a fresh recompute;
- coverage counts are reported per `counterexampleKind`, never as a single blended total.

## The five pilot candidates

Chosen to exercise every branch of the contract, not five of the easy one. If the pipeline handles these
five it handles the shape of the whole inventory; if it handles only the first, it proves nothing.

| # | Item | Kind | Branch exercised |
|---|---|---|---|
| 1 | `dtd:element/AffixTemplates` in `loader-isactive-breadth` | Surface | Word counter-example, parse to no-parse (`'takul': ok::TA+KUL\|takul -> ok::-`) |
| 2 | `dtd:element/AllomorphCoOccurrenceRules` in `morphotactic-attribute-breadth` | Surface | Word counter-example, the OTHER direction: no-parse to parse (`'sol': ok::- -> ok::SOL\|sol`) |
| 3 | `dtd:element/AffixTemplate` in `diacritic-segments` | Surface | `counterexampleKind = LoadFailure` — the weaker class that must not blend into the headline |
| 4 | `prAlpha`/`prHighTrigger` adjacent swap in `feature-system-breadth` | Ordering | Generated item, not DTD-derived; needs `disjoint-domains` if no delta |
| 5 | `dtd:enum/Stratum/cyclicity/cyclic` | Surface | The Proof branch — no evidence exists and none should be manufactured |

Candidate 2 matters because a counter-example that only ever removes a parse would leave the pipeline
untested against evidence that ADDS one, and 47 of the corpus's deltas are that direction.

## Cost

Measured, not estimated: bare process startup is 110-150ms, a 4-word fixture evaluates in 350-380ms, a
27-word fixture in 460ms. The sweep runs one evaluation per surface PER FIXTURE — about 1540 of them,
not 194 — and 1540 x 0.35s reconciles the observed 541s.

Startup is therefore only ~30% of a mutant's cost, and batching alone lands at 230-340ms, missing a
200ms target. Three levers, in order of what they actually buy:

1. **Stop at the first differing word.** Every mutant currently parses every word in the fixture, but
   `Evidenced` needs exactly ONE counter-example. `Unobservable` still parses everything, which is
   correct: it is the verdict that must look everywhere before claiming there is nothing to see.
2. **Stop at the first fixture that evidences a surface.** ~1540 evaluations for 194 surfaces is ~8x
   redundancy, and the ledger already keeps only the strongest verdict per surface.
3. **Batch mutants per child process**, amortizing startup toward zero.

The child process itself is not negotiable — it is what bounds memory after abandoned in-process work
took this machine to 37GB committed. Batching keeps the kill at batch granularity with a per-mutant
watchdog inside; a batch that hangs is killed and retried one-at-a-time, so the safety property survives
and only the pathological case pays the old price.

Levers 1 and 2 shrink the NUMBER of parses rather than the cost of each. Both change what the ledger
records, so each must state plainly that it stopped early and why — a run that quietly skips work reads
exactly like a run that covered it.

**Measured, and it retires the 200ms target.** Over the 138-item ordering sweep: 201.6s wall clock for 158
evaluations, averaging 1276ms, against 350-460ms measured on small fixtures — the spread is fixture size,
with `deep-optional-affix-nesting` alone at 7.6s. Lever 1 was measured directly rather than projected: over
the 31 evidenced items, 202 of 578 words could have been skipped, **34.9%** — and only on evidenced items,
which are 22% of the total. Batching removes ~120ms of ~1276ms, roughly 10%.

So 200ms per evaluation is NOT reachable for whole-fixture evaluation: process startup alone is 110-150ms
of it, and the parse itself exceeds the remainder on every fixture measured. Realistic is 250-350ms for
typical fixtures, and large ones will never approach it. The honest lever is not making each evaluation
faster but running fewer of them — stopping at the first fixture that evidences a surface, since ~1540
evaluations currently cover 194 surfaces.
