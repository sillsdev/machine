# Coverage levels, and what the suite does not measure

`docs/coverage-strategy.md` is the governing statement for how this suite's coverage claim is built
and apportioned; where anything below disagrees with it, this document is stale. Its "level 1"/"level
2" below are that document's **unit** layer; its "level 3" discussion is the reasoning that led to
splitting what was one open question here into two mechanical layers there — **integration/edge**
(`conformance/interface-inventory.tsv`, landed) and **integration/chain** (in progress) — plus a
capped hand-crafted set. The analysis below (why pairs are not enough, the MPR worked example,
preconditions vs. gaps, cost vs. correctness) is unaffected by that split and is why it is kept in
full rather than folded away.

The coverage numbers in this repository describe **individual grammar surfaces**. They do not
describe interactions between surfaces, and nothing here currently measures those. This document
states the levels precisely so a green suite cannot be read as a stronger claim than it supports.

## The levels

| Level | Unit | Target | State |
|---|---|---|---|
| 1 | a surface exists in the DTD | complete by construction | 1059 enumerated |
| 2 | a surface is load-bearing | evidenced, **or** classified impossible-to-evidence with a reason | 194 of 264 grammar-observable |
| 3 | an interaction between surfaces | enumerate fully, cover selectively, **name what is uncovered** | not enumerated |

Level 2's numbers reconcile exactly, which is worth keeping true: 264 grammar-observable surfaces
minus the 70 in `conformance/semantic-coverage-baseline.txt` leaves 194, and
`conformance/semantic-coverage-counterfactuals.tsv` holds exactly 194 rows — 106 `Evidenced`, 7
`EvidencedJointly`, 65 `RequiredByDtd`, 13 `RequiredByLoader`, 3 `Unobservable`. Every covered
surface has a verdict.

**`RequiredByDtd` and `RequiredByLoader` are not the same strength of evidence, and treating them
as one bucket ("`RequiredToLoad`", 78 surfaces) used to overstate this level's headline.** Both are
verdicts reached when neutralizing a surface stops the grammar from loading at all -- but *why* it
fails to load differs, and the difference is checkable mechanically from which kind of
neutralization produced it (`CounterfactualGate.LoadFailureVerdict`):

- `RequiredByDtd` (65 surfaces, all whole-element removals): the mutant fails **generic XML DTD
  content-model validation** (`XmlLanguageLoader`'s `ValidationType.DTD`) before a single line of
  HermitCrab's own loader runs. This re-derives what Level 1's static DTD enumeration already
  knows -- a Rust engine that loads every such grammar and then silently ignores the construct at
  parse time is indistinguishable from a correct one on this verdict alone. There is **no
  parse-time witness of any kind** for these 65 surfaces.
- `RequiredByLoader` (13 surfaces, all enum-value rewrites to a DTD-legal sibling): the document
  passes DTD validation and then HermitCrab's own loader throws -- an IDREF lookup, a
  feature/coercion failure, or similar. This is a genuine engine-semantic witness: the failure
  could only happen because the loader actually consulted the surface, even though (like
  `RequiredByDtd`) no word ever parses to show a word-level delta.

So the honest headline for level 2's strongest evidence is **106 `Evidenced` + 7
`EvidencedJointly` = 113 surfaces out of 264** carry a real parse-time delta. The other 78
`RequiredByDtd`/`RequiredByLoader` surfaces are resolved (not gaps) but split unevenly in strength:
13 are a genuine, if word-free, engine-semantic witness, and 65 have no parse-time witness of any
kind and only restate Level 1.

"Complete" at level 2 cannot mean "every surface evidenced". 78 surfaces are `RequiredByDtd` or
`RequiredByLoader`: deleting or rewriting them stops the grammar loading, so no word can
demonstrate their behaviour, and 3 are `Unobservable`. Completeness means every surface is either
evidenced or explicitly classified as impossible to evidence, with the reason recorded — the
standard the counterfactual ledger already applies.

## Interfaces: a DTD-fixed denominator, not a corpus statistic

Level 1 counts surfaces; level 3 wants interactions between them. Between the two sits a narrower
question that does not need a grammar to answer: which surfaces are *declared to point at another
surface at all*? `conformance/interface-inventory.tsv` answers it from the DTD alone — every
`IDREF`/`IDREFS` attribute in `HermitCrabInput.dtd` is one declared interface, 60 of them across 28
elements, resolved against the corpus only to learn which element types each one actually targets
and whether any fixture exercises it (42 of 60; 18 are declared but never used, `conformance/
interface-inventory.tsv`'s exercised column names them). That resolution yields 51 distinct
(element, attribute, target-type) edges, and exactly two target types —
`MorphologicalPhonologicalRuleFeature` and `PartOfSpeech` — are reached by both a write-direction
and a read-direction edge, which is where a chained interaction (something writes a payload another
construct later reads) is structurally possible at all.

This is deliberately not `conformance/rule-interaction-pairs.tsv`'s role. That ledger enumerates
rule *instances* within whatever fixtures exist, so its row count grows every time a fixture is
added and 93% of its rows are `Undetermined` by construction — useful for per-grammar pruning of
which adjacent swaps need a counterfactual, but not a fixed denominator. The interface inventory's
"60 declared" cannot move by adding a fixture; only "exercised" and "typed edges" can, which is what
makes it the right denominator for "how much of the schema's declared cross-referencing has the
corpus ever touched."

## Level 3 is not a level, and pairs are not enough

The instinct to define level 3 as "pairs of surfaces" fails on a defect this suite actually has.
An MPR feature group declared `outputType="overwrite"` does not accumulate: a later rule writing the
same group evicts the earlier rule's contribution. The hazard is not the `overwrite` value; it is
the conjunction of **four** things — an `overwrite` group, a stratum whose
`morphologicalRuleOrder` is `unordered`, two rules that both write that group, and a downstream
rule gated on `requiredMPRFeatures` naming the evicted member. Two derivations over the same
morpheme set then end with different MPR state and the gate fires in one order only.

All four ingredients are individually `Evidenced`. A pairwise enumerator would report every pair
inside that conjunction as covered and still miss it.

So arity is not the axis. Enumerating to arity 4 is not an option either: 264 observable surfaces
give 34,716 pairs, ≈3.03 million triples and ≈198 million 4-tuples.

**The unit must be structural, not combinatorial.** Enumerate interactions that can actually
co-occur in a grammar — rules sharing a stratum, attributes on mutually-relevant declarations, a
group's output policy against the rule order of the rules that write it — at whatever arity that
structure implies. An MPR group and the stratum order of its writers are structurally related; the
cross product of 264 surfaces is not. Ordered rule pairs within a stratum, for instance, are 190
for 20 rules and sum linearly across strata rather than exploding.

## Three states, not two

For each enumerated interaction:

1. **Cannot interact** — environments provably disjoint. Retired with evidence.
2. **Can interact, witnessed** — a real word where both fire and the order is load-bearing.
3. **Can interact, unwitnessed** — the actionable signal.

State 3 needs one further bit to be diagnostic rather than merely alarming: is the interaction
**reachable** in this grammar — does its gating ever let both fire on one form? Reachable and
unwitnessed is a conformance gap worth a fixture. Unreachable means the grammar's own gating
prevents it. Without that bit the enumerator over-reports every co-declared pair, and a gap list
that over-reports is ignored exactly like any other noisy gate.

Deciding "can interact" is not a scan of the XML. It requires intersecting one rule's output
language against another's trigger context — a real automaton construction. That analysis is not
built here.

## Witnessing an interaction

Observation alone cannot establish that order is load-bearing; a trace shows only that both rules
fired. The evidence is a counterfactual: swap the two rules' order, or neutralize one, and show the
delta. `PhaseTraceRecorder` makes that delta observable **per phase**, which matters because
HermitCrab unapplies rules in reverse — feeding in synthesis is bleeding in analysis, and an
interaction witnessed only in synthesis is untested in the direction a proposer runs.

## Selection discipline

Level 3 is covered selectively and deliberately. Two rules keep that honest:

- **Typology first.** Fixtures are derived from real morphological phenomena, then indexed by the
  interactions they happen to exercise — never authored to fill a gap row. A suite written to
  satisfy a metric measures the metric.
- **Uncovered means named.** The value of enumerating level 3 is not that it gets covered; it is
  that an uncovered interaction sits in a file with a name, instead of being discovered by accident
  later. Measurement is automatic; the decision to pin any particular interaction stays human.

## Admissible grammars, and why preconditions are not gaps

Some behaviour is not a coverage gap but a **precondition on the grammar**, inherited from the
reference engine's contract. The distinction is sharp and worth stating: a precondition *scopes* the
claim; a gap *falsifies* it. An undischarged, unstated precondition is a gap.

- **Every phoneme used in the orthography must be declared.** An undeclared segment makes the
  reference engine refuse every word containing it. That refusal is a defined outcome, not a defect,
  and the suite pins it as `expect_skip` (`InvalidShapeException` → `SKIPPED`), distinct from
  "well-formed input, zero analyses". This precondition is **per engine**: the AMPLE-family default
  parser requires only the phonemes used in natural classes or environments, so the requirement
  belongs to the capability profile and never to a universal sentence.
- **A segment-changing rule needs a fully specified, unique feature bundle per segment.** Two
  segments sharing an identical bundle leave the reference engine unable to determine which morpheme
  is involved. The suite can only ever claim fidelity to whatever the engine deterministically does,
  not to what the author intended, so this is recorded as an authoring precondition.
- **An ambiguous multigraph resolves longest-match-first.** There is no algorithmic remedy; the
  documented fix is to change the orthography.

None of the three is mechanically checked **as a precondition**, and saying so is the point — an
unstated precondition is indistinguishable from a gap, so each must be either enforced or listed as
unenforced, never left in between:

| Precondition | Enforced? |
|---|---|
| every phoneme in the orthography is declared | **no** — but the engine's *refusal* when it is violated is pinned (`expect_skip` fixtures), so the consequence is covered while the requirement is not |
| each segment has a unique, fully specified feature bundle | **no** — prose only; the reference tooling offers a manual inspection, and a lint could check it mechanically |
| multigraph ambiguity is acceptable to the author | **no** — unenforceable in principle; longest-match is the engine's behaviour, and the remedy is outside the grammar |

The first row is the distinction worth keeping in view: pinning what happens when a precondition is
broken is not the same as checking that it holds.

## Correctness and cost are different claims

A conformance verdict is a diff over final observable outcomes, so a hazard that changes only how
much work the engine does is **invisible to it** — not merely hard to test. Of eleven independently
documented engine hazards, seven leave the final parse identical. No fixture can witness those by
outcome, and conformance should not pretend to.

Cost claims need a different evidence shape: deterministic counter deltas (rule applications, parse
events) rather than wall clock, and for an asymptotic claim a **parameterized family** measured at
several sizes, since `O(2ⁿ)` is a statement about a family at increasing n and not about any one
fixture. The single exception already inside scope is cost that crosses into behaviour — a crash —
which is an observable outcome and is pinned by `expect_crash`.

## What is not built

**Updated by `docs/coverage-strategy.md`'s split.** The reachability analysis and the general
interaction ledger described above are still not built. The interface-declaration half of this
question is: `conformance/interface-inventory.tsv` now enumerates every DTD-declared
`IDREF`/`IDREFS` handoff (60, 42 exercised, 18 named gaps), so "which surfaces are declared to point
at another at all" is answered mechanically. What remains not built is reachability/witness proof for
those 18 gaps and the chain layer (does a written payload survive to its reader) — in progress as
`conformance/interaction-chains.tsv`. The honest statement is narrower than it was: uncovered
*edges* are named; uncovered *chains* have not been looked for yet, and one would not have been
noticed.
