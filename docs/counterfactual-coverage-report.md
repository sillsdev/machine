# Counterfactual coverage: what is evidenced, and what is not

**A point-in-time report, since superseded on headline numbers.** This records the sweep and the bugs
it caught (mismeasured `RequiredToLoad`, `Unobservable`, and `disjoint-domains` verdicts, each fixed
below) at 194 surfaces / 27 fixtures and 138 ordering items / 29 lists. The fixture set and verdict
split have both grown since: `docs/pangloss-handoff.md`'s "Current numbers" section and
`conformance/semantic-coverage-counterfactuals.tsv` hold the current 194-surface breakdown (106
`Evidenced` + 7 `EvidencedJointly` = 113, plus 65 `RequiredByDtd`, 13 `RequiredByLoader`, 3
`Unobservable` — `RequiredToLoad` below is that later-split 65+13 bucket under its old, merged name),
and `tests/.../OrderingGeneratorTests.cs` pins the current ordering count at 32 lists / 146 pairs. The
bug narrative and proof-kind reasoning below are unaffected by either change and are the reason this
report is kept rather than replaced.

## The standard

Every grammar-observable DTD surface must end in one of two states. There is no third.

1. **Evidenced** — a counterfactual delta, mechanically checked. The surface is neutralized in a copy of a
   fixture's grammar and the parse outcome changes. The delta is recorded per surface.
2. **Proven impossible** — an explicit claim, of a kind the gate recomputes rather than reads.

Anything else fails the gate. Prose is not a proof.

Neutralization is typed: delete the element, rewrite an enumerated value to a declared sibling, or remove
the attribute when the value IS the DTD default (the validating parser then supplies it, which is what
makes such a surface unobservable and is itself the proof).

## Verdicts

Measured over 194 grammar-observable surfaces from 27 fixtures.

| Verdict | Meaning | Count |
|---|---|---|
| `Evidenced` | neutralizing it changed a parse result | 106 |
| `RequiredToLoad` | the mutant would not load at all | 77 |
| `EvidencedJointly` | evidenced only with its referencing partner, with a necessity check | 3 |
| `Timeout` | did not terminate; not evidence | 0 |
| `Unobservable` | neutralizing it changed nothing; not evidence | 8 |

Cost, determinism and the properties a gate depends on:

- 7017 mutant word parses, 379 unmutated, roughly 7 minutes.
- Zero words exceed 2s, mutated or not. The 20s kill is a safety net that never fires, kept wide so a
  merely slow mutant is never mistaken for a non-terminating one and recorded as evidence.
- Deterministic. `Morpher.Synthesize` sizes its `Parallel.ForEach` off `Environment.ProcessorCount`, so a
  pathological mutant threw from one worker nondeterministically and a verdict could flip between runs.
  The child runs with `DOTNET_PROCESSOR_COUNT=1`; consecutive checks agree.
- Each mutant runs in a killable child process. Waiting on an in-process task does not stop a
  non-cancellable parse: abandoned mutants took the machine to 20GB working set and 37GB committed.

`EvidencedJointly` is weaker than `Evidenced` and ranked as such. It exists because four surfaces are
resolved by the loader through an eager, throwing `Dictionary` indexer, so a live reference to an inactive
declaration makes the UNMUTATED baseline fail to load. The joint mutation activates the declaration and its
referencing partner together, and credits the target only when all three hold: target alone changes
nothing, partner alone changes nothing, both together change a result. That establishes the target is
necessary rather than merely present.

## The eight not evidenced

### Proven impossible (2)

**`dtd:enum/Stratum/cyclicity/cyclic`**
**`dtd:enum/Stratum/phonologicalRuleOrder/simultaneous`**

Neither identifier appears anywhere in `src/SIL.Machine.Morphology.HermitCrab`. `Stratum.cs` carries
`MorphologicalRuleOrder` and no counterpart for either. `XmlLanguageLoader.LoadStratum` never reads them,
not even into a discarded local. Beyond the grep: `SynthesisStratumRule` and `AnalysisStratumRule` build
the stratum's phonological pipeline as an unconditional `LinearRuleCascade`, so there is no branch point a
stratum-level ordering mode could ever be consulted at.

Empirically: setting both attributes on `edge-cases/simultaneous-epenthesis-cascade` — a fixture engineered
to spiral into `InfiniteLoopException` under iterative application, i.e. maximally sensitive to exactly
this semantics — produced a byte-identical exception and stack trace.

**Should we stop trying? Yes.** No grammar shape can make an attribute observable that no code consults.

**Is it acceptable? Not entirely — this is a product gap, not a testing gap.** These are not vestiges of a
removed feature. Per-rule `multipleApplicationOrder` is real and implemented; stratum-level cyclicity would
mean re-running a stratum's cascade against its own derived output, a standard concept in this
architecture's home literature, and it exists nowhere. A grammar author reading the DTD would reasonably
expect it and get a silent no-op. Severity moderate: the defaults match what the engine always does, so
nobody is silently wrong unless they deliberately set `cyclic` or `simultaneous`. Worth an upstream ticket.

### Fixture work, not impossible (6)

None of these six is a proof. Each is a surface no current fixture discriminates, and each has a known
route to evidence that has not been built yet.

**`dtd:enum/MorphologicalOutput/redupMorphType/suffix` — RESOLVED, and it was our bug.** It was twice
claimed impossible: first as "no consumer" (false; it reaches `case ReduplicationHint.Suffix` in
`SynthesisAffixProcessAllomorphRuleSpec.cs:74`), then as "the mutator structurally cannot evidence it".
The second claim was true of the mutator and false of the engine. `GrammarMutator.Sibling` rewrote an enum
value to ONE arbitrary sibling — the ordinal-first — and for `suffix` that is always `implicit`, which the
engine treats identically. Nothing ever tried `prefix`, which discriminates outright:
`ok::KIMB+RED|kimbiakimbia` -> `ok::RED+KIMB|kimbiakimbia`. Enum neutralization now tries EVERY declared
sibling, short-circuiting on the first that yields evidence, and records which sibling won and which were
tried. The fixture's own comments asserted the false conclusion and have been corrected.

**`dtd:element/Properties` — PROVEN, kind `not-in-signature`.** Loaded at `XmlLanguageLoader.cs:469,517,
912,981,1048`, copied by value at `Morpher.cs:403,423`, and read nowhere else. `SignatureFormat.
BuildSignature` — the sole builder of the string two parses are diffed by — reads only morpheme ids and
shape. What is MECHANICAL: a source scan asserting the signature builder never references the property
(pinned by a control test proving the scanner does flag `Id`, which the signature genuinely reads), plus a
real mutation of every fixture containing the element, requiring every word's outcome to be unchanged.
What remains ASSERTED: that no other engine path branches on a `Properties` value, which is a reading of
the cited call sites, not whole-program dataflow. The proof records both halves separately, because a
proof that blurs the checked and the assumed is the failure this whole system exists to prevent.

**`dtd:enum/ComplexFeature/isActive/no`**, **`dtd:enum/SegmentNaturalClass/isActive/no`**,
**`dtd:enum/SymbolicFeature/isActive/no`**, **`dtd:enum/FeatureNaturalClass/isActive/no`** are the
catch-22 shape: the declaration is deliberately referenced by nothing, because a reference would dangle in
the baseline. That is a harness limitation, not an impossibility, and the joint-mutation mode exists
precisely to resolve it. Three of a first four are already resolved that way.

RESOLVED. All four now carry an inactive partner in `edge-cases/feature-system-breadth` that is their sole
referent — `prDecFeat`, `eDecCf`, `prDecNc`, `prDecFnc` — and all four evidence `EvidencedJointly` through
`CounterfactualGate.EvaluateJointly` itself, not by hand:

| Surface | Leg 1 target alone | Leg 2 partner alone | Leg 3 joint | Word |
|---|---|---|---|---|
| `SymbolicFeature` | Unobservable | RequiredToLoad (`feature 'featDecoy' could not be found`) | `ok::AK\|ik` -> `ok::-` | `ik` |
| `ComplexFeature` | Unobservable | RequiredToLoad (`feature 'cfDecoy' could not be found`) | `ok::-` -> `ok::TAL\|tal` | `tal` |
| `SegmentNaturalClass` | Unobservable | RequiredToLoad (`'ncDecoy' not present`) | `ok::SAL\|sal` -> `ok::-` | `sal` |
| `FeatureNaturalClass` | Unobservable | RequiredToLoad (`'ncFDecoy' not present`) | `ok::AK\|ik` -> `ok::-` | `ik` |

What actually blocked these was a defect in the harness, not in the fixtures. `FindJointPartner` searched
`DescendantsAndSelf()` for a nested reference, but `LocatePartner` — which both `MutatePartnerAlone` and
`MutateJointly` use to re-find that partner — still only checked attributes on the partner element itself.
None of these four references sit there: they are on a nested `VariableFeature`, `SimpleContext` or
`FeatureValue`. So a pair was located and then its mutations could not be built, and the surface was
recorded `Unobservable` — a tooling failure wearing the costume of a fact about the engine. The same
mismatch had been silently defeating the pre-existing `loader-isactive-breadth` pair too.

## Rule ordering

Surfaces were never the whole inventory. A stratum's rule list and an affix template's slots are ordered,
and nothing verified that the order is load-bearing: the sweep neutralizes one surface at a time and never
permutes anything.

Ordering is now generated as first-class inventory — 138 items, one per ADJACENT pair across 29 ordered
lists. Adjacent transpositions generate the symmetric group, so pinning every adjacent swap pins the total
order; the two 16-rule lists cost 15 items each rather than 2x10^13 permutations, and the whole sweep runs
in 201.6s with zero timeouts and zero load failures.

| Result | Count | |
|---|---|---|
| Evidenced — swapping changed a word | 31 | 22% |
| Proven independent (`disjoint-domains`) | 13 | 9% |
| GAP — no delta, independence unproven | 94 | 68% |

**The 94 are the honest headline.** A gap cannot distinguish "these rules are independent" from "our words
are too weak to tell", so it is not a pass. 84 of them fall in two list kinds the static check does not
model at all (72 morphological-rule pairs, 12 template-slot pairs) and 6 are pairs the check resolves as
genuinely OVERLAPPING — those can never be proven independent, so a discriminating word is their only
possible resolution.

That reasoning was right about heuristics and wrong about the corpus. 53 of those pairs sit in strata
declaring `morphologicalRuleOrder="unordered"`, which compiles to a `CombinationRuleCascade` trying every
rule at every step and collecting into a set — permutation-invariant BY CONSTRUCTION, so no discriminating
word can exist for them. Three exact proof kinds followed: `unordered-invariant` (53), `inactive-member`
(9, a rule that never loads cannot be ordered against), and `pos-disjoint` (3, since `partOfSpeech` is a
singular IDREF so no root satisfies two disjoint gates). The line worth keeping is not "never prove" but
"prove only exact structural facts, never approximations."

### A certified pair that was not independent

The falsification test — for every item, is it BOTH structurally certified AND empirically evidenced? —
found one, and one is enough to condemn a proof kind:

`prNasalAssimAlveolar~prObstruentDeletion` was certified `disjoint-domains` on the grounds that its output
`{cNlv}` and the next rule's input `{cK,cS,cT}` do not intersect. Swapping them changes `menulik` from
`ok::NPFX+TULIK|menulik` to `ok::-`. `prObstruentDeletion` deletes only inside a nasal LeftEnvironment, and
`cNlv` IS nasal: the first rule CREATES the environment the second one needs. Textbook feeding.

`CheckDisjointDomains` compared outputs against inputs and never read `Environment`/`LeftEnvironment`/
`RightEnvironment` at all — so feeding and bleeding, the two classic reasons order matters, were invisible
to the one check licensing "these rules cannot interact." The design doc specified it that way; the
implementation was faithful to a spec that was wrong. The corrected rule widens both sides: the later
rule's SENSITIVE set includes its environments, and the earlier rule's EFFECT set includes what it consumes
as well as what it produces, so bleeding counts as well as feeding.

The lesson is not that the prover was buggy. It is that a proof kind needs an adversarial check that can
falsify it against the real engine, because a false certification is silent, permanent, and indistinguishable
from coverage.

## Claims that did not survive verification

Recorded because the failure mode matters more than the count.

- **`label-symmetry` for `VariableFeature/name` was FALSE.** The claim was that 24 Greek letters are
  interchangeable labels. Alpha variables are unified across every `VariableFeature` a rule declares, not
  scoped per declaration, so renaming one to collide with another reverses which words parse. Verified by
  applying the gate's own mutation: `ia` → `AU|ia` becomes no-parse, and `au` no-parse becomes `AU|au`. The
  proof was deleted. Lesson recorded in the file: a symmetry claim needs a grammar where two of the values
  CO-OCCUR before it can be believed. Testing one variable in isolation is how the error survived.
- **Four joint surfaces were reported evidenced with a hand-verified table, and three were not.** The probe
  words that produced the deltas were deliberately not added to `words.yaml`, so the sweep never parsed
  them. Evidence that exists only outside the fixture is not evidence.
- **`Properties` and `redupMorphType` were reported no-consumer.** Both are consumed; see above.
- **Every "impossible" verdict so far has been a defect in this tooling, not a fact about the engine.**
  Four in a row: `LocatePartner` could not see a reference nested inside a partner element, so four joint
  pairs were located and then silently unbuildable; `Sibling` tried one arbitrary enum value, so a synonym
  masked a real difference; four surfaces had no inactive partner to pair against at all; and the enum
  short-circuit preferred whichever evidence came first alphabetically over the stronger kind. Only
  `Stratum/cyclicity` and `phonologicalRuleOrder` survived scrutiny, and those are an upstream product gap
  rather than a coverage one. `Unobservable` has been functioning as the place where our own bugs go to
  look like conclusions, which is precisely why it must fail the gate rather than rest there.
- **A proof asserted independence from the fixture's WORDS.** The ordering pilot's `disjoint-domains`
  claim read "no fixture word puts a member of ncHighCons before an 's'". That is the GAP condition, not a
  proof: one added word falsifies it, and until then it licenses the pair as independent. The conclusion
  happened to be right for a different, structural reason (`prAlpha`'s output cannot intersect
  `prHighTrigger`'s input), and the check is now recomputed from the grammar rather than authored. It
  appeared inside the pilot for the system built to prevent it, which is the measure of how easy it is.
- **Trace evidence over-credited `MetathesisRule@multipleApplicationOrder`.** The rule fired, so the value
  was credited; flipping the value changes no outcome. Rule-level attribution shows the rule ran, not that
  the value mattered. This is the gap the counterfactual standard exists to close, and it caught its author.

## What the gate enforces

`hc-conformance --counterfactual` exits non-zero when a surface is neither evidenced nor claimed in
`conformance/semantic-coverage-proofs.tsv`, when a proof names a surface that is now evidenced or no longer
measured, or when the checked-in ledger disagrees with a fresh recompute. It runs on demand and weekly
rather than per push, because a minutes-long gate on every push is one that gets switched off.
