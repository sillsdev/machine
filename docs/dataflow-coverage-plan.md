# Data-flow and MC/DC coverage for the conformance suite

The interaction layers built so far declare *what could interact*. This plan states *how much of it
must be demonstrated*, using two external criteria rather than a private judgment, so the resulting
number can be evaluated against forty years of literature instead of taken on trust.

An architecture review corrected six things in the first version of this plan. The corrections are
kept visible below rather than quietly folded in, because most of them are instances of a failure
mode this programme keeps repeating and the record is more useful than a clean document.

## The criteria, and why these two

**Data-flow coverage** (Rapps and Weyuker, 1985) fits the chain layer:

| data-flow term | this suite |
|---|---|
| *def* — writes a variable | `LexicalEntry.ruleFeatures` writes an MPR feature |
| *use* — reads it | `PhonologicalSubrule.requiredMPRFeatures` reads it |
| *kill* — redefinition before the use | an overwrite MPR group, and three others (below) |
| *def-clear path* | a chain with no mutator between write and read |

The MPR-overwrite defect that motivated this layer is, in these terms, an ordinary *definition killed
before use*. The hazard class is understood and the criteria for catching it already exist.

**MC/DC** (DO-178C) fits the gate case: a condition must be shown to affect the outcome
*independently*, demonstrated by cases differing only in that condition.

## The target

**All-uses, plus MC/DC on every gate, plus every kill-path witnessed.**

Stopping short of all-du-paths is deliberate. The honest justification is the classical one from the
data-flow literature itself — path explosion and the cost of proving infeasible paths — **not** NIST
SP 800-142, which the first version cited. That citation was out of domain: SP 800-142 is about
input-parameter interaction in combinatorial testing, not path coverage, and read on its own terms it
argues the other way. A (writer, mutator, reader) triple is a 3-factor interaction, squarely inside
its 1-3 factor band, so it *mandates* the full kill-path space rather than licensing a short list.

Recorded for honesty: the criterion was chosen with the baseline number already in hand
(`interaction-chains.tsv` landed 2026-08-14, this plan 2026-08-15). That does not make all-uses the
wrong bound, but the bound is not prior to the measurement and should not be presented as if it were.

## The measured baseline, and why the first version of it was wrong

| criterion | first published | **witness-grade** |
|---|---|---|
| all-defs | 6/7 | **5/7** |
| all-uses | 20/40 | **11/40** |
| kill-paths | 2 of 2 | **2 of 24** |

The first column used `interaction-chains.tsv`'s `exercised` column, which is a **static
co-occurrence check** — the same id appearing in both attributes' values somewhere in one fixture,
with no ordering, no reachability and no parse-level evidence. Several "exercised" chains have
endpoints that are provably inert in the only fixture that cites them: the `ruleFeatures` to
`HeadMorphologicalInput` chains are exercised solely by `fusional-realizational-morphology`, where
both readers are `Unobservable` across all 61 words.

So the plan's own baseline table was computed with the weak predicate the plan exists to abolish.
That is the fourth time this suite has published presence as though it were evidence — after the
`RequiredToLoad` verdict, the interface inventory's `Exercised` column, and fixture design. It is
worth naming the pattern rather than just the instance: **the weak predicate is always the one that
is already computed, and it is always cheaper to reach for.**

`Timeout` rows in the witness ledger count as **not witnessed**. "I could not look" must never read
as "fine."

## What gets built

**1. An obligation matrix.** For each chain, derive the cases required to demonstrate it, emitted as
a generated, drift-gated ledger with stable cell identifiers.

- **There is no "plain def-use" class. All 40 chains are gates.** The first version classified a
  chain as gated when the reader was a `required*`/`excluded*` attribute — a name-prefix heuristic,
  the same move that `SemanticInterfaceDirection` exists to replace. Twelve chains have
  `head*`/`nonHead*` readers and every one gates in engine code. Gate-ness is classified from the
  engine's gate shape, never from the attribute name.
- **MC/DC obligations are per condition, not a fixed 2x2.** An HC gate is not one boolean:
  `MprFeatureSet.IsMatchRequired` is a conjunction over features when ungrouped or `matchType="all"`
  and a disjunction when `matchType="any"`, and `CompoundMprFeaturesMatch` is a different shape again
  (vacuous truth on empty, then disjunction). A gate on n features needs at least n+1 cases with each
  condition independently toggled; a 2x2 toggles the set as a whole, which is decision coverage. The
  corpus already contains a two-condition gate (`requiredMPRFeatures="mprP mprQ"` under
  `matchType="all"`). The 2x2 is the n=1 special case.
- **A cell is satisfied only by a pair witness**: severing the writer flips a named word *and*
  severing the reader flips that same word. Two independent edge facts — writer witnessed somewhere,
  reader witnessed somewhere, possibly on different words in different fixtures — is edge coverage
  twice, and certifying a composition from it is the exact fallacy the chain layer exists to kill.

**2. Cell claims in `words.yaml`**, additive so no consumer can break: PanGloss's parser sets no
`deny_unknown_fields` and carries a comment that it tolerates upstream schema additions.

**3. A gate**: every required cell of every claimed chain must be claimed by a named word *and*
pair-witnessed.

**4. Filling the coverage**, preferring extension of a language-family grammar over a new fixture,
per `docs/coverage-strategy.md`.

## Decisions

**Scope: all four mutator classes.** Not a cost judgment — the inclusion rule in
`docs/coverage-strategy.md` decides it. Each of the four demonstrably changes a parse, so each earns
obligations. POS replacement matters most in practice: derivational morphology changes category and
templates gate on category, so it is the most common mutation in any real grammar, and it currently
has zero obligations.

**Target: ratchet from 11/40, aiming high.** The fraction is published and gated against decrease,
and chains close as grammars are extended. Aim at all of them rather than at a comfortable subset —
but expect some to be unreachable, and treat that as a finding to establish rather than assume.

**Unreachable needs a proof, not a shrug.** The first version of this plan had no infeasibility
escape, which would have left every hard chain looking identical to every impossible one. A chain
that cannot be witnessed by any valid grammar must carry an impossibility proof in the manner of
`ImpossibilityProofs` at the surface layer — a recorded, checkable reason, recomputed rather than
trusted. Until such a proof exists, an unwitnessed chain is a gap, never an exemption. Engine reads
and writes exist for every endpoint checked so far, so the expectation is that most or all 40 are
feasible and the proof set stays small.

**Order dependence and the analysis direction: named exceptions for now.** Recorded above as outside
what these criteria express, pinned only by the existing hand-crafted fixture, revisited after this
layer lands.

**Fold-in: the 30 first.** Thirty obligations sit in grammars that already contain the construct
structurally and need only extra words — no new grammar content, so they are taken first. The 57
needing genuinely new grammar content are judged individually against whether the construct is
natural to that grammar's shape; a construct bolted onto a grammar that would never have it makes
the grammar less realistic and is worth less than the fabricated fixture it replaces.

## The contract: what the machine proves, and what a human certifies

This is the hardest part of the layer and the part everything else rests on.

The machinery proves a **flip**: severing an attribute changed a word's outcome. For the worked cell
that is `removed ruleFeatures from 1 <LexicalEntry>` and `'vokadan': ok::- -> ok::VOKAD+SUF|vokadan`
— no parse becomes a parse once the payload is gone, so the gate was doing the blocking.

It does **not** prove the **role attribution** — that `vokadan` occupies the `PresentGatedForm` cell
rather than some other cell of the matrix. That needs three facts the flip does not carry: the stem
carries the payload, the named rule is the gated one, and "blocked" is the expected polarity for an
`excluded*` reader. Part of that is mechanisable; part is not — the mutator work found readers where
a witness exists but no role can be attributed, and those cells stay `Unknown` by design.

**The role attribution is the contract.** Everything else is derivation.

Two consequences follow, and both are requirements rather than niceties.

**A reviewer must be able to say yes in one read.** Today the judgment needs a hand-join across four
artifacts — the cell from `dataflow-obligations.tsv`, the delta from `interface-witness.tsv`, the
author's prose from `words.yaml`, the gate from `grammar.xml`. Nobody performs that 346 times, which
means in practice nobody performs it once. The remedy is a rendered evidence card per cell: the role
restated in plain English, the exact mutation, the before-and-after parse, and the author's own note.

**A human "yes" must expire when its evidence moves.** A sign-off carries a hash of the machine
evidence it was given — the mutation and the delta. If those change, the attestation is stale and the
cell reverts to unreviewed. Witnessed and reviewed are different facts and are never collapsed into
one status: the first is machine-established, the second is human-asserted, and only the first
recomputes itself. An attestation that outlives what it certified is the same defect this programme
has now published five times in other forms, wearing a signature.

## The mutator classes

The kill-path denominator is schema-derived — chains x mutator classes — never corpus-derived. The
first version set `Hazardous` only where an exercising fixture already contained an overwrite group,
which makes the denominator grow when a fixture is added: precisely what the governing strategy
document forbids.

Four engine-verified ways a payload is altered in transit. Only the first is modelled today:

1. **Overwrite MPR group** — `outputType` anything but `append`.
2. **POS replacement** — `SynthesisAffixProcessRule` priority-unions `OutSyntacticFeatureStruct`, so
   an intervening `outputPartOfSpeech` kills a `LexicalEntry.partOfSpeech` def before an
   `AffixTemplate.requiredPartsOfSpeech` read. Structurally identical to the MPR defect, and the most
   common mutation in any realistic grammar.
3. **Blocking** — `Word.CheckBlocking` rebuilds the word from another entry's primary allomorph;
   `SetRootAllomorph` clears the MPR feature set and reseeds, killing the MPR, POS and StemName
   payloads at once. Reachable from every blockable rule.
4. **Compounding non-head drop** — the output word is built from the head, so a non-head's
   `ruleFeatures` def is dropped at the boundary with no overwrite group present anywhere.

## Named exceptions: what these criteria cannot express

Per this repository's own rule that a layer which cannot name what it misses is wrong:

- **Order dependence under an unordered stratum.** All-uses is existentially quantified — one
  def-clear-path witness discharges a pair — so once one interleaving is witnessed the ledger goes
  green while another still fails. `mpr-overwrite-order-dependence` pins exactly such a case. Neither
  all-uses, all-du-paths, nor MC/DC expresses it; it lives only in a hand-crafted fixture.
- **The analysis direction.** Chains are derived and judged in the synthesis direction only. Nothing
  in the chain layer speaks to unapplication.

## What this deliberately does not do

No all-du-paths. No covering array over the full interface set: tools exist (NIST's ACTS, Microsoft's
PICT) but the parameters here are heavily constrained — most combinations are not valid grammars — so
an unconstrained array would mostly emit grammars that cannot load. A combinatorial layer, if ever
wanted, needs constrained covering arrays and belongs above this one rather than instead of it.

The gate validates **the test suite**, never the engine. A chain being unwitnessed is a statement
about the corpus. Nothing here asserts that HermitCrab is correct.
