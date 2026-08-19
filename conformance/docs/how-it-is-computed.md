# How coverage is computed

`what-it-claims.md` gives the headline numbers. This document explains where each denominator comes
from and how a numerator is earned — the mechanics behind the claim, not the claim itself.

## The governing rule: denominators come from the DTD and the engine, never from the corpus

Every layer below answers "out of how many possible things" before it asks "how many of them does
the corpus demonstrate." That first number is always computed from `HermitCrabInput.dtd` (what a
grammar could legally declare) or from the engine's own source (what it actually reads), and never
by counting what fixtures happen to contain. The reason is structural: a denominator computed from
the corpus grows every time someone adds a fixture, which lets the coverage fraction rise by adding
easy cases instead of hard ones, and makes two runs of the suite at different fixture counts
incomparable. `conformance/rule-interaction-pairs.tsv` is the cautionary example on record — it
enumerates rule-interaction *instances* across whatever fixtures currently exist (1,305 rows at last
count, up from 1,290 when four fixtures were added), so it cannot serve as a coverage denominator and
is not used as one anywhere in this suite; see `decisions-and-lessons.md` for the full story of why
it was demoted to a different job.

## Presence is not coverage — the idea everything else depends on

A construct can be declared in a grammar, resolve correctly, and load cleanly, and still change
nothing when the grammar runs — invisible to any engine that only compares parses, and therefore
worth nothing to a claim meant to transfer to a different implementation. Concretely, from
`conformance/interface-witness.tsv`: the `AffixTemplate.requiredPartsOfSpeech` attribute is
*present* in six fixtures, but severing it (removing the attribute and re-parsing every word)
changes no result in four of them —

```
AffixTemplate  requiredPartsOfSpeech  edge-cases/deep-optional-affix-nesting               Unobservable
AffixTemplate  requiredPartsOfSpeech  edge-cases/diacritic-segments                        Unobservable
AffixTemplate  requiredPartsOfSpeech  languages/suffixing-extension-slot-ordering          Evidenced
AffixTemplate  requiredPartsOfSpeech  languages/suffixing-vowel-harmony                    Evidenced
```

— and changes one in the other two, where a specific named word's signature flips from a real parse
to none once the attribute is gone. Both kinds of fixture *declare* the attribute; only the second
kind *witnesses* it. Every layer below is built to keep that distinction visible rather than collapse
it, because collapsing it is a mistake this suite has made and re-made — see
`decisions-and-lessons.md`.

The mechanical test behind "witnessed" is always the same shape, called a **counterfactual**: take a
fixture that declares a surface, neutralize it (delete the element, blank the attribute, remove the
enumerated value — whatever "gone" means for that surface), and re-parse every one of the fixture's
words against the mutant grammar. Four things can happen, and only one of them counts as evidence:

| verdict | what happened | counts as evidence? |
|---|---|---|
| `Evidenced` | a named word's parse changed | yes — this is a real, word-level witness |
| `EvidencedJointly` | only a *joint* mutation with an independent partner changed a result | weaker; recorded, not counted alone |
| `RequiredByLoader` | the mutant fails to *load* — HermitCrab's own loader threw after DTD validation passed | a genuine engine-semantic fact, but no word was ever reached |
| `RequiredByDtd` | the mutant fails generic DTD content-model validation, before the loader even runs | re-derives the DTD's own content model; proves nothing about the engine |
| `Timeout` | the mutant did not finish in time | no — a timeout is not evidence, and is never read as if it were |
| `Unobservable` | every word parsed identically with the surface removed | no — this is the presence-without-witness case above |

(Verdict definitions per `conformance/semantic-coverage-counterfactuals.tsv`'s own header.) A
`Timeout` is deliberately never treated as passing or as inconclusive-but-fine: "the sweep could not
look" must never read as "nothing is wrong there."

## Layer 1: unit surfaces — 264 grammar-observable, 110 demonstrated

**Denominator.** Every element `HermitCrabInput.dtd` allows to appear, and every value an enumerated
attribute can legally take, read directly off the DTD (`semantic-coverage-baseline.txt`'s own header:
"an element that can appear, or an enumerated attribute value that can be written"). This is a static
property of the schema; adding a fixture cannot change it.

**Numerator.** `semantic-coverage-counterfactuals.tsv` runs the counterfactual test above once per
surface a fixture actually declares, and the number reported as "demonstrated" is the count of
`Evidenced` (plus the weaker `EvidencedJointly`) verdicts — 110 of 264. The rest are not silently
absorbed: 66 are `RequiredByDtd`, 13 are `RequiredByLoader` (both real facts, neither a parse-time
witness), and every remaining surface carries either an explicit **impossibility proof**
(`semantic-coverage-proofs.tsv` — a proof kind the gate recomputes, such as `dtd-default`: the value
equals its attribute's own DTD default, so a validating parser supplies it whether or not a fixture
writes it, and no word could ever discriminate that) or sits, named, in
`semantic-coverage-baseline.txt` as an acknowledged gap. `constructs.txt`'s eleven
engine-unimplemented surfaces (cyclic strata, syntactic subcategorization, cross-word phonological
context — see `conformance/README.md`) resolve here too, as `no-consumer`/`dead-schema`: proven, not
by a fixture, but by scanning the engine's own source for a reference to the element and finding
none.

## Layer 2: interface edges — 60 declared, 42 present, 15 demonstrated

**Denominator.** Every `IDREF`/`IDREFS` attribute `HermitCrabInput.dtd` declares, across 28 elements
— 60 of them, read straight off the DTD (`interface-inventory.tsv`'s header). Each is a typed
handoff: one construct pointing at another (an `Allomorph.stemName` pointing at a `StemName`, a
`CompoundingRule.headPartsOfSpeech` pointing at a `PartOfSpeech`). IDREFs are untyped in a DTD, so
the *target type* for each attribute is not read from the schema at all — it is discovered by
resolving every real reference across the whole corpus and recording what element type answers on
the other end (`observed_target_types` in `interface-inventory.tsv`).

**Present versus demonstrated.** 42 of the 60 resolve to a real reference somewhere in the corpus —
"present," structural only, exactly the presence half of the distinction above. The demonstrated
count, 15, comes from `interface-witness.tsv`: the same counterfactual test as layer 1, run once per
(element, attribute, fixture) triple where the interface is present, by severing every occurrence of
that attribute in that fixture and re-parsing. The gap between 42 and 15 is the size of a mistake
this suite made once already: an earlier version reported the 42 itself as if it were coverage, and
`interface-witness.tsv` exists specifically to correct that (`decisions-and-lessons.md`).

**Direction is engine-checked, not name-derived.** Whether a given interface *writes* a payload,
*reads* it (gates on it), or merely *refers* to it structurally is decided by
`SemanticInterfaceDirection`, a table verified against the engine's own read/write sites — not by an
attribute-name convention (does it start with `output`, `assigned`, `required`...). The
name-convention version existed first and was shown wrong by a real field grammar that writes zero
`MorphologicalOutput.MPRFeatures` and instead carries every inflection-class feature through
`LexicalEntry.ruleFeatures`, an attribute the naming convention calls "ref" even though the loader
unions it straight into the same live `MprFeatures` set every reader gates on. See
`decisions-and-lessons.md` for the fuller account; the fix is the reason interaction chains (layer 3)
exist as their own denominator rather than being read off `interface-inventory.tsv` directly.

## Layer 3: interaction chains — 40, 11 demonstrated

A single interface edge in isolation cannot show what happens to a payload *between* one rule
writing it and another reading it — whether something sits in the path that overwrites, drops, or
otherwise destroys the value first. That composition is what this layer measures.

**Denominator.** Group every declared interface by the payload type it carries (using the
engine-checked direction from layer 2, not the name-prefix heuristic), and keep only payload types
reached by at least one writer *and* at least one reader — a **chain junction**
(`interaction-chains.tsv`'s own header). One row per (writer edge, payload type, reader edge) at each
junction: 40 total, including junctions no fixture exercises yet, so an entirely untested reader
still appears in its own denominator rather than silently vanishing from it.

**Numerator.** A chain counts as exercised when the same identifier appears in both the writer's and
the reader's attribute values in one fixture — a real but purely **static co-occurrence check**, not
proof that the payload actually reaches the reader unmutated. `dataflow-coverage-plan.md` gives the
honest number after correcting for that: of 40 chains, the `all-uses` data-flow criterion (Rapps and
Weyuker, 1985 — a def-clear path from writer to reader, not merely both present) is met by 11, down
from 20 in a first published count that used the weaker static check. The suite deliberately targets
`all-uses` rather than the stronger `all-du-paths` (every path, not just one) — a scoping choice made
for the classical reason in the data-flow literature (path explosion, and the cost of proving
infeasible paths infeasible), recorded honestly as a choice made *after* the baseline number was
already in hand rather than argued for in advance.

**Hazardous.** A chain is flagged `hazardous` in the ledger when at least one exercising fixture also
contains a known **mutating construct** on that payload's path — today, an `outputType="overwrite"`
MPR feature group. This is what layer 4 turns into obligations.

## Layer 4: obligation cells — 346, 2 demonstrated

**Denominator.** For every chain in layer 3, derive the specific test cases that would demonstrate
it, per `dataflow-coverage-plan.md`'s obligation matrix — this is the layer where MC/DC enters:

- **Every chain gates — there is no plain, ungated def-use case.** A reader "gates" when it makes a
  control-flow decision based on the payload, and that turns out to be true of all 40 chains, not
  only the ones literally named `required*`/`excluded*`: `CompoundingRule.headPartsOfSpeech` gates
  via `Unify` in engine code exactly as a `required*` attribute does. So every chain contributes the
  MC/DC floor of four cells — `PresentControl`, `PresentGatedForm`, `AbsentControl`,
  `AbsentGatedForm` — cell kind `McDc`.
- **MC/DC is per condition, not a fixed 2×2.** Where a real fixture's attribute value lists more than
  one feature id under a conjunctive or disjunctive match (`requiredMPRFeatures="mprP mprQ"` is a
  real, checked-in case), MC/DC over *n* conditions needs *n+1* cases, not 2 — cell kind
  `ConditionExtension` adds exactly the extra vectors that specific fixture's condition count
  requires, attributed to the fixture that realizes it, never assumed.
- **Mutator cells** add a `MutatorAbsent`/`MutatorPresent` pair per chain for each schema-applicable
  **mutator class** — an engine-verified way a payload is altered in transit before the reader sees
  it:
  1. **Overwrite** — an `outputType="overwrite"` MPR feature group drops the rest of its group before
     unioning in a new value. The only class with a corpus detector today.
  2. **Blocking** — a blocked derivation is rebuilt as a new `Word` from a sibling lexical entry,
     clearing and reseeding MPR features, syntactic features, and the root allomorph (hence stem
     name) all at once. Reachable from any rule, since every rule defaults `blockable="true"`.
  3. **POS replacement** — an intervening rule's own `outputPartOfSpeech` priority-unions over a
     part-of-speech definition placed earlier in the derivation, before a later
     `required*PartsOfSpeech` read sees it. The most common mutation any realistic derivational
     grammar would contain, and it currently has zero satisfied obligations.
  4. **Compounding non-head drop** — a compound's output is built from the head alone, so a
     non-head's entire MPR feature set is never copied forward at all; no overwrite group is even
     needed for its definition to fail to reach a later reader.

  Only class 1 is modeled by a corpus-wide detector; the other three are declared and scoped but not
  yet automatically found in a fixture.

346 cells in total.

**Numerator, and what it actually requires.** A cell is `Satisfied` only by a **pair witness**:
severing the *writer* flips a named word's outcome, **and** severing the *reader* flips that *same*
word — not two independent facts (writer witnessed somewhere, reader witnessed somewhere, possibly on
different words in different fixtures), which would be edge coverage counted twice and is exactly the
fallacy this layer exists to rule out. Two cells meet that bar today. Every other cell is
`NotSatisfied` (the chain isn't even structurally exercised yet) or `Unknown` (structurally exercised,
but the pair witness hasn't been established) — both are named as gaps, never quietly folded into a
passing total.

**What a cell being satisfied does *not* prove.** The counterfactual machinery proves a **flip** —
severing an attribute changed a word's outcome. It does not by itself prove **role attribution**:
that the flipped word actually occupies the specific matrix cell claimed (that the stem carries the
payload, that the named rule is the gated one, that the observed polarity is the expected one for that
reader). Attribution needs a human judgment on top of the machine-proved flip; a `words.yaml` word can
declare a `claimed_cells` entry naming which cell it witnesses, but that claim is checked against the
recomputed ledger, never taken on trust, and a sign-off is invalidated the moment the underlying
mutation or delta it was reviewed against changes. `conformance/evidence-cards/` renders one card per
cell precisely so this judgment can be made in one read instead of a four-artifact hand-join — see
`how-it-works.md`.

## What none of these four layers measure

Stated here because it bears directly on how to read the numbers above, and is stated at more length
in `what-it-claims.md`: nothing across these four layers measures a surface *in combination with*
another surface it wasn't specifically paired with (that is exactly what layers 3 and 4 exist to
start correcting, one payload type at a time, not a claim that the job is finished), and none of them
speaks to the *analysis* (unapplication) direction — every chain in layer 3 and every cell in layer 4
is derived and judged in the synthesis direction only. Order dependence under an unordered stratum is
also outside what any of these four layers can express at all, existentially quantified criteria being
satisfied by one witnessed interleaving even when a second, unwitnessed one would fail — that hazard
class is pinned only by a hand-crafted fixture (`edge-cases/mpr-overwrite-order-dependence`), never by
a ledger.
