# Handoff: the HermitCrab conformance suite, for a consuming engine

This closes the HC semantic-products programme's PanGloss milestone
(`docs/superpowers/plans/2026-08-13-hc-semantic-products-implementation-plan.md`, Tasks 1-9 shipped,
7/8 cut, 12 deferred). It states exactly what a consumer receives, how to read it, and — because the
programme's own Status section is explicit that a stronger product was considered and dropped — what
was deliberately not built and why. Every number below was read from a checked-in file at handoff
time; none is estimated.

## What is handed over

Two files per fixture, 33 fixtures total (8 under `conformance/languages/*`, 25 under
`conformance/edge-cases/*`):

- `grammar.xml` — a `HermitCrabInput` document, format `sil-machine-hermit-crab-input-xml/v1`.
- `words.yaml` — the fixture's cases, format `hc-conformance-words/v1`. This is the **single
  canonical authored format**: front matter (`language`, `inspired_by`, `sources`, `requires`) plus
  one entry per word, each an `expect_fail`, `expect_skip`, or one-or-more `parses` (each a
  `signature`/`gloss`/`rules`/`exercises`). Case data — inputs, parses, rejections, skips,
  `neutralizes` — lives **only** here. There is no generated JSON restatement of it; one existed
  (`hc-conformance-corpus/v1`) and was deleted, because it was larger than its own sources while
  carrying less: the authored files hold 1011 comment lines of derivation reasoning no generated JSON
  can express, and two representations of the same ground truth meant a permanent drift gate.

Alongside the fixtures:

- `conformance/HermitCrabInput.dtd` — the published grammar DTD, byte-identical to the library's own
  embedded copy (`src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd`; a test holds the two
  equal). Published under `conformance/` specifically so a consumer that receives only that directory
  — which is exactly what PanGloss's sparse checkout does — can resolve the DOCTYPE without reaching
  outside it.
- `conformance/schema/conformance-manifest.schema.json` and `conformance/schema/words.schema.json` —
  Draft 2020-12, `additionalProperties: false`.
- `conformance/generated/hc-conformance-manifest.v1.json` — format `hc-conformance-manifest/v1`,
  13KB, one entry per fixture (`fixtureId`, `category`, `displayLanguage`, `grammarPath`,
  `grammarSha256`, `wordsPath`, `wordsSha256`, `caseCount`, `expectedCrash`) plus top-level `dtdPath`,
  `dtdSha256`, `sourceHash`, and the three format-version identifiers below. Currently 33 fixture
  entries, 444 cases total.
- `conformance/coverage.csv` (language x word x construct) and `conformance/rules.csv` (language x
  grammar rule id x exercising words) — both generated, both derived from the fixtures, never hand
  maintained.
- `conformance/constructs.txt` — the flat, cross-grammar checklist of grammar-model constructs every
  fixture's `exercises:` values are drawn from.
- `conformance/PROTOCOL.md` — the adapter contract: CLI shape (`<engine> batch <grammar> <words.txt>
  <output.tsv>`), the 5-column TSV format, the signature algorithm, comparison semantics, and
  capability profiles (§5, below).

The three format identifiers, exactly: `hc-conformance-manifest/v1`, `hc-conformance-words/v1`,
`sil-machine-hermit-crab-input-xml/v1`.

## How to consume it

Read `words.yaml` and `grammar.xml` directly for grammar and cases. Read the manifest only for
discovery and integrity — it is provenance (paths, SHA-256, case counts), not a restatement of any
case. Regenerate it with `hc-conformance --generate-manifest`; verify the checked-in bytes with
`hc-conformance --check-manifest`. Both flags validate every grammar against the DTD and every
`words.yaml` against `words.schema.json` before writing anything, so a manifest that exists at all is
already known-valid against both schemas.

**DTD resolution.** When loading a `grammar.xml`, admit only the system identifier
`HermitCrabInput.dtd` and map it to the path the manifest's `dtdPath` records
(`conformance/HermitCrabInput.dtd`). Refuse every other external entity, every network fetch, and
every filesystem fallback. A resolver that does anything else is not implementing this contract.

To validate an engine against the suite at all, implement `PROTOCOL.md` §1's adapter contract (a
`batch` command over `grammar.xml` producing the 5-column TSV in §2) and declare a capability profile
per §5 — see "Preconditions" below for what a profile scopes.

## The claim, scoped exactly

Passing means an engine reproduces the reference implementation's observable parse behaviour
(signature, status, or a pinned crash) on every fixture its declared capability profile admits.
Passing does **not** mean the engine can parse any language, and — this is the sharper, proven half
of the same sentence — it does not even mean the engine covers any grammar expressible in the DTD.
`grammar.xml` can legally declare `Stratum/cyclicity="cyclic"` or `Stratum/phonologicalRuleOrder=
"simultaneous"`; `conformance/semantic-coverage-proofs.tsv` proves the reference engine consults
neither identifier anywhere in its source (`no-consumer`, the strongest of the four proof kinds the
ledger recognizes). A language needing cyclic strata is legal XML and unimplemented by the founding
oracle itself, so "covers any expressible grammar" is false by the suite's own evidence, not merely
unproven.

The inventory this suite counts against is **formalism surfaces — DTD elements and enumerated
attribute values — never languages.** Nothing here quantifies over languages, and nothing should be
read as if it did. "Complete" is correspondingly an accounting claim: every inventoried surface
carries an explicit status (evidenced, evidenced-jointly, required-by-dtd, required-by-loader,
unobservable, or named as not yet resolved), never a claim that every grammar an implementer might
write is covered.

Coverage is also, deliberately, per-surface at its strongest layer, not per-interaction.
`docs/coverage-strategy.md` is the governing statement for how this whole claim is built and
apportioned across layers, and where anything below disagrees with it, it is stale. A surface counts
as covered (the **unit** layer) when a fixture exercises it and neutralizing it changes a real word's
parse. That says nothing, by itself, about two or more surfaces interacting: `docs/coverage-levels.md`
documents a real defect (an MPR `overwrite` group evicted by unordered-stratum rule order) whose
ingredients are each, individually, fully covered, and would still be invisible to a pairwise-or-lower
coverage claim — see that document for why arity is not the right axis.

Above the unit layer, `docs/coverage-strategy.md` names two further mechanical layers plus a
deliberately capped hand-crafted one. **Integration/edge** — does data cross a declared handoff at
all — is landed: `conformance/interface-inventory.tsv` derives 60 `IDREF`/`IDREFS` interfaces from
the DTD alone, of which 42 are exercised and 18 are not (see "Current numbers" below). **Integration/
chain** — does a payload survive from the construct that writes it to the one that reads it, at the
two junctions where a type is both written and read — is still being built; no chain count is landed
yet. **Hand-crafted** fixtures for what neither mechanical layer can derive are deliberately capped at
5-20. Do not read the absence of a chain-layer number as "unstarted"; it is in progress, just not yet
checked in.

## What is explicitly NOT handed over, and why

The semantic-inventory product (`hc-semantic-inventory/v1`, plan Task 8) does not exist and is not
coming. It was cut together with its prerequisite, Task 7 (execution-boundary auditing), for a
reason worth stating plainly so the scope cannot inflate later: that product would have censused the
C# engine's own internals — decision points, loader behaviour, source-level execution edges — rather
than the grammar format. The census splits cleanly into 1059 `dtd:` surfaces (the grammar language
itself, which this handoff ships in full) and 592 `decision:`/`loader:`/`model:`/`source:` surfaces
belonging to the engine's internals, which it would have required roughly 233 hand-written boundary
dispositions, plus permanent upkeep against every future change to the engine, to cover. And the
completeness claim that work would have served was unreachable regardless: `SIL.Machine` — where the
pattern matcher and feature-unification engine actually live — is never censused
(`GraphSemanticCensus.CensusedProjects = { "hc", "hc-tool" }`). The decision recorded in the plan's
Status section is that the grammar format and its interpretation are what matter to a consumer, and
that HermitCrab is the "golden" engine only in the sense that it happens to cover the whole grammar —
nothing about its internals is privileged enough to be worth a permanent, partial, hand-maintained
census.

Concretely: **the consumer receives the corpus half of the original two-product plan, not the
semantic-inventory half.** There is no engine-internals product, partial or otherwise, and no plan to
build one. Do not treat its absence as an open task.

## Current numbers, as verified against the files listed

- **Unit layer, DTD surfaces:** 1059, enumerated. Verified: `docs/coverage-levels.md`, and the plan's
  Task 7 note giving the same figure as the `dtd:` half of the 1059/592 split.
- **Unit layer, grammar-observable surfaces:** 264 total. Of those, 194 resolve to a verdict —
  counted directly from `conformance/semantic-coverage-counterfactuals.tsv` (194 data rows, one
  verdict column each): 106 `Evidenced`, 7 `EvidencedJointly`, 65 `RequiredByDtd`, 13
  `RequiredByLoader`, 3 `Unobservable`. **The honest headline is 106 + 7 = 113 of 264**: only those
  carry a real parse-time delta. `RequiredByDtd` and `RequiredByLoader` used to be reported as one
  merged 78-surface `RequiredToLoad` bucket; that overstated the claim; splitting them apart made
  `RequiredByDtd`'s 65 surfaces visible as re-deriving the DTD's own content model rather than
  evidencing anything the loader or engine did. The remaining 70 are listed, unresolved, in
  `conformance/semantic-coverage-baseline.txt` (70 non-comment lines). 194 + 70 = 264, and the
  arithmetic is exact, not rounded.
- **The exception list:** eleven of those surfaces are named exceptions rather than gaps — expressible
  in the DTD and read by no engine code at all, across three feature areas (cyclic strata and
  simultaneous phonological rule order; syntactic subcategorization, six surfaces; cross-word
  phonological context — `PreviousWord`, `NextWord`, `Null`). They are machine-derived, not asserted:
  two are `no-consumer` proofs in `conformance/semantic-coverage-proofs.tsv`, and nine are
  `dead-schema` lines in `conformance/semantic-coverage-baseline.txt`. See `docs/coverage-strategy.md`
  for the full list and the gate that keeps it from drifting.
- **Integration/edge layer:** 60 `IDREF`/`IDREFS` interfaces declared across 28 DTD elements — a
  DTD-fixed denominator, counted directly from `conformance/interface-inventory.tsv`. **42 exercised,
  18 not.** Seven of the 18 belong to the dead subcategorization feature above and are correctly
  uncovered; the rest are real gaps, one fixture each.
- **Integration/chain layer:** not yet landed. `docs/coverage-strategy.md` estimates roughly 15
  writer x reader chains through the two junctions where a type (`MorphologicalPhonologicalRuleFeature`
  or `PartOfSpeech`) is both written and read, but no chain ledger is checked in yet, so there is no
  number to report here beyond that estimate.
- **Ordering:** 32 ordered lists with >= 2 members, 146 adjacent pairs total across them. Verified
  against the pinned assertions in
  `tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/OrderingGeneratorTests.cs`
  (`Assert.That(totalLists, Is.EqualTo(32))`, `Assert.That(totalPairs, Is.EqualTo(146))`). This is a
  narrower, DTD-ordering-carrier-specific enumeration and is not the same denominator as
  `conformance/rule-interaction-pairs.tsv` below.
- **Fixtures:** 33 (8 `languages/`, 25 `edge-cases/`), 444 cases total. Verified by listing
  `conformance/languages/*` and `conformance/edge-cases/*` and by summing `caseCount` across all 33
  entries in `conformance/generated/hc-conformance-manifest.v1.json`.
- **Not a coverage denominator:** `conformance/rule-interaction-pairs.tsv` (1,305 rows, 1,217
  `Undetermined` by construction) is a per-grammar pruning ledger, not an interaction-coverage count.
  See `docs/coverage-strategy.md`'s "What this replaces" section. Do not cite its row count as
  interaction coverage.

Of the eleven exception-list surfaces above, the two cited earlier as the scoping example are named
exactly: `dtd:enum/Stratum/cyclicity/cyclic` and `dtd:enum/Stratum/phonologicalRuleOrder/simultaneous`,
both `no-consumer` in `conformance/semantic-coverage-proofs.tsv`.

## Preconditions on admissible grammars

Some behaviour documented in `docs/coverage-levels.md` is not a coverage gap but a precondition
inherited from the reference engine's contract — a precondition *scopes* the claim, a gap *falsifies*
it, and an undischarged, unstated precondition would be a gap by that same standard:

- **Every phoneme used in the orthography must be declared.** An undeclared segment makes the
  reference engine refuse the whole word (`InvalidShapeException`, pinned as `expect_skip` /
  `SKIPPED`, a defined outcome, not a defect). **This requirement is per engine, not universal**: the
  AMPLE-family default parser (XAmple, the motivating case for the capability-profile mechanism in
  `PROTOCOL.md` §5) requires declaring only the phonemes used in natural classes or environments, a
  strictly weaker requirement. A consumer's own capability profile determines which version of this
  precondition applies to it.
- **A segment-changing rule needs a fully specified, unique feature bundle per segment.** Two
  segments sharing an identical bundle leave the reference engine unable to determine which morpheme
  is involved — recorded as an authoring precondition, since the suite can only claim fidelity to what
  the engine deterministically does, not to what a grammar author intended.
- **An ambiguous multigraph resolves longest-match-first**, with no algorithmic remedy; the
  documented fix is to change the orthography.

## Delivery

Today, a consumer reads these files directly from the `conformance/` path of a sparse checkout of
this submodule — this is why the DTD is published inside `conformance/` at all, rather than left only
as the library's embedded resource. A content-only NuGet package (plan Task 12) is **deferred, not
cancelled**: it was not required to reach this milestone because the sparse-submodule path already
works, and remains available as future work if a non-submodule distribution channel becomes worth
building.

## A caution on coverage denominators

Three coverage efforts exist that a reader might be tempted to reconcile into one number. Do not:

- **This suite's 264 grammar-observable DTD surfaces** (unit layer above) — a census of the grammar
  format itself.
- **The consumer's own 23 characteristics x 3 backends = 69 pairs** — a census of which
  (characteristic, compilation backend) combinations the consumer has witnessed, has a gap for, or
  cannot represent at all (44 of 69 are gaps, as separately tracked on the consumer side).
- **`conformance/constructs.txt`'s rows** — a flat, informal checklist of named grammar-model
  constructs that fixtures' `exercises:` values draw from, only some of which map onto a
  characteristic at all.

These are three true statements about three different subjects, with three different denominators
and three different units of "coverage." A construct can be covered here, unmappable in the second
census, and simply absent from the third, all correctly, all at once. Never fold them into a single
combined percentage — doing so would misstate what every one of the three actually measures.
