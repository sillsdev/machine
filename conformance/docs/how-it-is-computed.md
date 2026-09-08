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

## Layer 2: interface edges — 60 declared, 44 present, 19 demonstrated

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

## Layer 3: interaction chains — 40 declared, 26 exercised, 9 with a paired witness

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
proof that the payload actually reaches the reader unmutated. Correcting for that: of 40 chains, the
`all-uses` data-flow criterion (Rapps and
Weyuker, 1985 — a def-clear path from writer to reader, not merely both present) is met by 11, down
from 20 in a first published count that used the weaker static check. The suite deliberately targets
`all-uses` rather than the stronger `all-du-paths` (every path, not just one) — a scoping choice made
for the classical reason in the data-flow literature (path explosion, and the cost of proving
infeasible paths infeasible), recorded honestly as a choice made *after* the baseline number was
already in hand rather than argued for in advance.

**Hazardous.** A chain is flagged `hazardous` in the ledger when at least one exercising fixture also
contains a known **mutating construct** on that payload's path — today, an `outputType="overwrite"`
MPR feature group. This is what layer 4 turns into obligations.

## Layer 4: obligation cells — 346 enumerated, 18 worth covering, 9 demonstrated

**Denominator.** For every chain in layer 3, derive the specific test cases that would demonstrate
it via an obligation matrix — this is the layer where MC/DC enters:

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

## A second denominator: engine gates, not DTD attribute pairs — 23 gates, 16 witnessed

Every layer above is keyed to the DTD: an attribute, or a pair of attributes joined by a payload
type. `conformance/engine-gate-inventory.tsv` is keyed to something else entirely —
`SIL.Machine.Morphology.HermitCrab.FailureReason` (`ITraceManager.cs`), the 23-member enum
(excluding `None`) HermitCrab itself uses to name every decision it makes when declining to apply or
unapply something. This is the engine's own vocabulary for "why did this not happen", authored by
whoever wrote the engine, not derived from the schema at all — a genuinely different axis from the
four layers above, not a fifth rung on the same ladder, so none of "what none of these four layers
measure" changes; this layer has its own separate blind spots, described below.

**Why a second denominator, not a replacement for the first.** The DTD-attribute layers answer "does
severing this payload flip a parse" — the right question for proving a schema-legal construct is
alive. But a schema-derived denominator can only ever name what the schema expresses, and the engine
makes some decisions the schema fuses or never names as its own thing:

- **Fused.** `FailureReason.RequiredStemName` is ONE engine gate reached from TWO unrelated DTD
  attributes on two different elements (`Allomorph.stemName`'s own check in `RootAllomorph.cs`, and
  `MorphologicalRule.requiredStemName`'s check in `SynthesisAffixProcessRule.cs`) — a DTD-attribute-pair
  ledger has no way to say these are "the same kind of failure," because its cells are organized by
  attribute, not by engine decision. `RequiredMprFeatures`/`ExcludedMprFeatures` are each reached from
  THREE attributes across three different elements the same way
  (`MorphologicalInput`/`HeadMorphologicalInput`/`PhonologicalSubrule`).
- **Unnamed.** `FailureReason.PartialParse`, `FailureReason.BoundRoot`,
  `FailureReason.MaxApplicationCount`, `FailureReason.DisjunctiveAllomorph`, and
  `FailureReason.SurfaceFormMismatch` have no row in `dataflow-obligations.tsv` at all, because that
  ledger's cells are payload write/read pairs and none of these five is that shape — `BoundRoot`, for
  instance, is a single-attribute boolean gate (`Allomorph.isBound`) with no reader-side attribute to
  pair it with, so a writer/reader-pair denominator cannot even pose the question.

**What turned out to have an attribute anyway, against this layer's own working assumption when it
was proposed.** The proposal guessed `PartialParse`, `BoundRoot`, `MaxApplicationCount`,
`SurfaceFormMismatch`, `DisjunctiveAllomorph`, and the two template-ordering reasons had no DTD
attribute reaching them at all. Reading `XmlLanguageLoader.cs` line by line for every one of the 23
raise sites (`EngineGateInventoryLedger.DtdAttributes`'s own doc comment cites the exact lines) showed
that guess half wrong: `BoundRoot` *does* resolve to one attribute (`Allomorph.isBound`),
`MaxApplicationCount` to one
(`MorphologicalRule.multipleApplication`/`CompoundingRule.multipleApplication`), `PartialParse` to two
(`Slot.optional`, `AffixTemplate.final`), and both template-ordering reasons to the same two
(`AffixTemplate.final`, `MorphologicalRule.partial`). Only `SurfaceFormMismatch` and
`DisjunctiveAllomorph` are genuinely attribute-free: the former is a pure engine-internal
reconstruction check with no grammar input at all, the latter is driven by allomorph order and
`Environment` element overlap among siblings, never a flat attribute value. Three more —
`Environments`, `Pattern`, `HeadPattern`/`NonHeadPattern` — join them for a different reason: their
DTD-side driver is child ELEMENT content (`RequiredEnvironments`/`ExcludedEnvironments`, a
`PhoneticSequence`), not an attribute, so `dtd_attributes` is honestly `-` for six gates in total, not
seven. Recording that a guess was wrong, and exactly how, is the point of writing the mapping down
mechanically-cited rather than asserting it.

**How `Witnessed` is decided, and why it is a WEAKER claim than a `Satisfied` obligation cell —
read this before citing either number as if they meant the same thing.** `EngineGateWitnessSweep`
runs every non-pathological, non-crash fixture's words through the reference engine with tracing on
and records, per word, every non-`None` `FailureReason` value that appears ANYWHERE in that word's
trace tree. A gate is `Witnessed` the moment one word's trace contains it once — full stop. That is
deliberately the weakest question this layer could ask, and for a specific reason:
`FailureRuleAttributor`'s own doc comment (in this same Conformance project) documents that several of
these reasons — `RequiredMprFeatures`, `RequiredSyntacticFeatureStruct` chief among them — fire
*routinely* for a rule that was merely tried against a candidate it has nothing to do with, in a
linear/unordered stratum's search, and that there is no `FailureReason` value that reliably tells
"this candidate specifically conflicts with the rule" apart from "the rule doesn't apply here, as it
structurally never would." A `Witnessed` row here means only **"some word made this gate fire"** — it
does **not** mean "severing a payload provably flipped a parse," which is what a `Satisfied` row in
`dataflow-obligations.tsv` means (a same-word pair witness: severing the writer AND severing the
reader both flip one named word from failed to successful). The two numbers answer different
questions at different strengths, and neither substitutes for the other: a `Witnessed` engine gate can
be true of a fixture that would still show `NotSatisfied`/`Unknown` on every dataflow-obligation cell
touching the same rule, and a rule with a `Satisfied` obligation cell does not automatically make every
engine gate on its path `Witnessed` either — they are simply different measurements, not points on one
scale.

**The trigger evidence is trace-derived, not a static guess.** `triggered_by_fixtures` and
`triggering_words` come from an actual engine run (`EngineGateWitnessSweep.Sweep`), not from reading
grammar.xml attribute values and inferring what "should" fire — the same standard `TraceRuleAttributor`
and `FailureRuleAttributor` already hold this suite's rule-level attribution to.

**16 of 23 gates are `Witnessed`.** The 7 that are not are each a specific, named corpus gap, not "the
suite is thin" in the abstract — see `EngineGateInventoryLedgerTests.UnreachedGatesAreTheKnownCorpusGaps`
for the exact reasoning behind each one (a compounding candidate whose head/non-head phonetic pattern
is tried and fails; a rule re-applied to its own `multipleApplication` cap within one derivation; the
non-head-side compounding gates, structurally present but never the one that actually fails; a
`partial="true"` rule tried immediately after an explicit `AffixTemplate final="false"`; and a rule
whose `outputObligatoryFeatures` promise goes unmet by the time `Morpher.IsWordValid` checks it).
## A third constraint layer: what a real FieldWorks project can produce

Everything above measures two layers: what the HC **engine** does, and what HC's XML/DTD can
**declare**. Neither speaks to a third, narrower constraint: what a real FieldWorks project can
**produce**. `HCLoader` (`Src/LexText/ParserCore/HCLoader.cs` in the separate FieldWorks repo) is the
single component that turns a LibLCM-backed FieldWorks project into an HC grammar — it is the only
place that integrates both the LibLCM data model and what is reachable from it, so it is the right
authority for this third layer. `conformance/fieldworks-producibility.tsv` records, for every
`FailureReason` the engine can report (`ITraceManager.cs`, 23 members besides `None`) and every
IDREF/IDREFS attribute this suite's own DTD inventory tracks (`interface-inventory.tsv`, 60 rows —
same 60 as layer 2 above), whether `HCLoader` can ever produce it.

**Why a `No` here is an exclusion, not a gap.** Layers 1–4 are all in the business of finding gaps to
close — an unwitnessed surface is something a future fixture could still demonstrate. This layer is
the opposite kind of fact: if `HCLoader` never emits a construct, no FieldWorks user session can ever
produce it, no matter how the grammar is authored, so no fixture — however cleverly written — can make
covering that construct say anything about real FieldWorks use. A `No` here does not mean "not yet
covered"; it means the coverage question is moot for that subject, and the honest response is to
retire it from the "should the suite cover this" conversation rather than keep chasing it. 22 of the
83 subjects landed there. About a third of those (9 of 22, on the interface side) are constructs the
*engine* fully implements and even the engine's own reference XML loader reads — `HCLoader` simply
never wires them up from LibLCM (`CompoundingRule.outputObligatoryFeatures`, `.outputProdRestrictions
MprFeatures`, `HeadMorphologicalInput.requiredMPRFeatures`/`excludedMPRFeatures`, `InsertSegments.` /
`Segments.characterDefinitionTable`, `LexicalEntry.family`, `MorphologicalRule.
outputObligatoryFeatures`, `SymbolicFeature.defaultSymbol`) — a FieldWorks-specific exclusion layered
on top of an engine that could support them. The rest were already dead schema even in the engine's
own loader (the subcategorization/`SyntacticRule` family, plus `LexicalEntry.morphologicalRules` and
the two `obligatory*Features` attributes), so `HCLoader`'s absence there is inherited, not new.

**Method.** Every verdict required reading the whole of `HCLoader.cs` (~2837 lines) and tracing each
subject to the specific runtime property it would need to set on this repo's own engine classes,
using `XmlLanguageLoader.cs` (this repo's reference HC-XML loader) as ground truth for which property
a DTD attribute maps to where the mapping isn't obvious from the name alone, and this repo's own
`InteractionChainLedger.cs` comments as corroboration for which attributes are dead even in the
engine's native loader. A `No` is backed by a full-file search for the property name and its plausible
spellings (never a single failed grep), and cites what was searched in
`conformance/fieldworks-producibility.tsv`'s `notes` column. `HCLoaderTests.cs` (FieldWorks repo) is
cited as corroborating evidence where it exercises a construct directly.

**Generation.** The subject list — every `FailureReason` and every interface-inventory attribute — is
extracted mechanically by `conformance/tools/generate-fieldworks-producibility.ps1` from this repo's
own sources (`ITraceManager.cs` and `interface-inventory.tsv`), so it can never go out of sync with
either by hand-typing. The **verdict** for each subject cannot be generated the same way: it required
a human reading of `HCLoader.cs`, a file in a different repository, and is embedded in the generator
script as a researched table. The script's only automated guarantees are that every mechanically-
extracted subject has a recorded verdict (missing one is a hard failure, not a silent default), that
no verdict is stale (recorded for a subject that no longer exists), and that the output has no
duplicate subjects — exactly the internal-shape guarantees `FieldworksProducibilityLedgerTests.cs`
re-checks against the checked-in file.

**The weakness this cannot fix.** Every other ledger in this document is computed from files that live
*in this repository* (the DTD, the engine source, the fixture corpus), so a test here can recompute
the ledger from scratch and fail loudly the moment it drifts from the checked-in file — that is what
`CheckedInInterfaceInventoryLedgerIsUpToDate` and its siblings do. This ledger cannot work that way:
its authority, `HCLoader.cs`, lives in the FieldWorks repository, which is not a dependency of this
one and must never become one (a test that opens a sibling checkout on disk breaks for every
contributor who does not have that repo cloned next to this one, and CI has no reason to have it at
all). So `conformance/fieldworks-producibility.tsv` is a **snapshot at a point in time**, not a
continuously verified derivation. If `HCLoader.cs` changes — a new attribute gets wired up, an old one
stops being read — nothing in this repo's test suite will notice. The only tests that exist
(`FieldworksProducibilityLedgerTests.cs`) validate the checked-in file's own internal shape (every
subject present exactly once, every `producible` value recognized, a `Yes` cites at least one site, a
`No` cites what was searched) against sources already inside this repo; they cannot and do not attempt
to re-derive a verdict from the FieldWorks repo. Treat this ledger the way you would treat a citation
to an external paper: trustworthy as of when it was written, and due for a re-read — by hand, not by
CI — whenever `HCLoader.cs` is known to have changed.

## Gate-keyed obligations: MC/DC over the engine's own 23 gates — the primary claim now

`conformance/gate-obligations.tsv` (`GateObligationLedger`) is the synthesis of the two sections
above: it takes the engine-gate denominator (23 `FailureReason` gates) and, for each one, demands
the two arms MC/DC requires, evidenced with the engine's own vocabulary rather than inferred from a
DTD attribute's spelling. `what-it-claims.md`'s primary funnel table gives the headline counts; this
section is the mechanics behind them.

**Why this ledger exists, restated precisely.** `dataflow-obligations.tsv` emits four MC/DC arms per
writer/reader chain but `FindPairedWitness` derives the arm it can certify purely from the reader
attribute's own name (`required*` → `AbsentGatedForm`, `excluded*` → `PresentGatedForm`, anything
else → nothing) — of 346 cells, 28 are even certifiable, and it cannot name a gate the schema fuses
into no single attribute at all (six of the 23 gates have no row there whatsoever — see the
"second denominator" section above). `GateObligationLedger` starts from the engine's 23 gates
instead, so every gate gets a denominator row regardless of what the schema calls it.

**The two arms, and how each is evidenced.**

- **Blocked.** A word whose parse fails, whose own trace tree (walked exactly as
  `EngineGateWitnessSweep` already walks it) names this gate's `FailureReason` directly, and where
  severing the DTD attribute or attributes `EngineGateInventoryLedger.DtdAttributes` maps to this
  gate (`InterfaceWitnessGate.Sever`, the same severance primitive `interface-witness.tsv` is built
  on) flips that *same* word to a successful parse. This is a strictly stronger claim than a
  `dataflow-obligations.tsv` chain pairing: the trace attributes the failure to the gate BY NAME, so
  there is no need to infer an arm from an attribute's spelling, and no risk of crediting a flip to
  the wrong one of two gates that happen to share an attribute (e.g. `AffixTemplate.final` feeds both
  `PartialParse` and the two `NonPartialRule*AfterTemplate` gates — a flip is only credited to the
  gate whose own `FailureReason` the blocked word's trace actually contains).
- **Control.** A word — anywhere in the same fixture — where the SAME grammar rule instance that fed
  the Blocked arm's gate fires in a *successful* parse (`TraceRuleAttributor` over the rule id
  `GrammarRuleIndex` resolves for that XML element), proving the rule can apply at all and that the
  Blocked arm's failure is attributable to the gate rather than to a rule that structurally never
  runs. Only resolvable when the gated construct sits directly on `MorphologicalRule`,
  `CompoundingRule`, or `RealizationalRule` — an `Allomorph`, a co-occurrence rule, or a template/slot
  construct (`AffixTemplate`, `Slot`) has no "Applied" trace event to check a rule id against at all,
  the same documented gap `FailureRuleAttributor`'s own doc comment records for allomorph identity.
  When the writer element is not one of those three, the Control arm is reported `NotEvidenced`
  naming exactly that limitation — never guessed, and never silently downgraded to "Unknown".

**The two layer verdicts, and why a miss says WHERE.** Every obligation also carries:

- **`xml_reachable`** — whether any DTD attribute or element reaches the gate at all. Read directly
  off `EngineGateInventoryLedger.DtdAttributes`: `-` means no attribute, but five of its six `-`
  entries ARE still reachable via element content (`Environments` via child elements, `Pattern`/
  `HeadPattern`/`NonHeadPattern` via `PhoneticSequence` content, `DisjunctiveAllomorph` via allomorph
  sibling order) and are marked `xml_reachable=Yes` on that basis. Only `SurfaceFormMismatch` is
  `xml_reachable=No`: it is a pure engine-internal reconstruction check (`Morpher`'s confirming-
  synthesis shape comparison) with no grammar attribute or element input at all.
- **`flex_producible`** — FieldWorks' own HCLoader capability, read straight from
  `fieldworks-producibility.tsv` (treated as fixed, per that file's own doc comment — this ledger
  does not re-derive or drift-check it). Only `ObligatorySyntacticFeatures` is `flex_producible=No`.

An obligation is `worth_covering` only when both are `Yes` — 21 of 23 gates (42 of 46 obligations)
today. A gate that IS worth covering can still be `NotEvidenced` for reasons that have nothing to do
with either layer, and each is named distinctly rather than collapsed together:

- **Unreached in the current corpus.** 6 of the 7 `Unreached` gates from `engine-gate-inventory.tsv`
  are worth covering but no fixture triggers them at all yet (the seventh, `ObligatorySyntacticFeatures`,
  is also `flex_producible=No`, so it is excluded before reaching this check). No candidate word
  exists to evidence either arm from, so both are `NotEvidenced` naming the corpus gap directly.
- **No severance primitive for a bare element-content gate.** `Environments`, `DisjunctiveAllomorph`,
  and `Pattern` are `xml_reachable=Yes` via element content and ARE witnessed by the corpus, but this
  ledger has no isolable way to sever "this one rule's `PhoneticSequence` shape" or "this allomorph's
  sibling order" without touching unrelated rules — a real tooling gap, named as exactly that, not
  attempted and not guessed.
- **Severance found, but the trace never names this gate for the flipped word.** The gate's
  `FailureReason` may fire only as routine noise against a candidate structurally unrelated to a
  word's final result (`FailureRuleAttributor`'s own documented phenomenon) — severing the attribute
  can still flip an unrelated word's outcome for a different reason, and this ledger refuses to credit
  that as evidence for THIS gate.
- **Control-only: the writer element is not rule-indexed.** Even when the Blocked arm IS evidenced,
  the Control arm reports `NotEvidenced` by name whenever the gated construct's element
  (`Allomorph`, `MorphologicalInput`, `AffixTemplate`, `PhonologicalSubrule`, a co-occurrence rule)
  is not one of the three `GrammarRuleIndex` can resolve. `HeadProdRestrictMprFeatures` is the one
  gate today whose blocking attribute (`CompoundingRule.headProdRestrictionsMprFeatures`) sits
  directly on a `CompoundingRule` element, so it is the only gate with both arms `Evidenced` —
  contrasting it against, say, `BoundRoot`'s `Allomorph`-rooted Control arm is the check that the
  Control arm's limitation is about the ELEMENT KIND, not a broken mechanism (see
  `GateObligationLedgerTests.OnlyHeadProdRestrictMprFeaturesHasBothArmsEvidenced`).

**Where the severance evidence comes from.** For every DTD attribute that is also an `IDREF`/`IDREFS`
attribute (`interface-inventory.tsv`'s own scope), `GateObligationLedger` reads the already-computed
`interface-witness.tsv` rather than re-running the engine. For the small number of gates whose DTD
attribute is boolean (`Allomorph.isBound`, `Slot.optional`, `AffixTemplate.final`,
`MorphologicalRule.partial`, `*.multipleApplication`) — never an `IDREF`, so `interface-witness.tsv`
never covers them — it falls back to a fresh `InterfaceWitnessGate.Evaluate` run, the exact same
severance primitive, just not already cached in a checked-in file. Either way, the attribution check
(does the candidate word's own baseline trace name this gate) is always a fresh, in-process traced
parse — one per fixture, shared across every gate that touches it, so a corpus-wide sweep costs one
traced pass per fixture rather than one per (gate, fixture) pair.

**Cost, and why the tests read the checked-in file.** `GateObligationLedger.Compute` runs a real
traced engine sweep (one parse per word, per fixture any worth-covering gate triggers) plus a
severance re-parse per Blocked-arm candidate — the same order of cost as
`EngineGateInventoryLedger.Compute`. `GateObligationLedgerTests`' assertions all read
`GateObligationLedger.Read` (the checked-in file); only `CheckedInGateObligationLedgerIsUpToDate`
recomputes, and it is `[Explicit]` — the same convention `EngineGateInventoryLedgerTests` already
established, and the same one `dataflow-obligations.tsv`'s own tests do NOT need, because
`DataflowObligationLedger.Compute` never runs the engine at all (it only re-derives schema-derived
chains and reads already-checked-in witness data).
