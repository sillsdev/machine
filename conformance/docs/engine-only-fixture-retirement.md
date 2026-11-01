# Engine-only fixture retirement: decision record

**Decision (owner).** The conformance suite keeps only grammars a real FieldWorks project can
produce, that is, fixtures marked `fieldworks_producible: true`. A fixture that needs a construct
`HCLoader.cs` (FieldWorks `ParserCore`) cannot emit tests the engine against input FieldWorks never
sends, so it cannot catch a regression a FieldWorks user would see. Such a fixture is converted
to a producible grammar when that preserves its purpose, and deleted when it does not.
`ConformanceFixtureGateTests.EveryFixtureIsMarkedFieldWorksProducible` enforces the rule.

This replaces the earlier analysis, which sorted the then-remaining 36 `false` fixtures into
CONVERT / DELETE / KEEP-ENGINE-ONLY. No fixture is kept engine-only any more.

## Result

| | Fixtures |
|---|---|
| Before | 151 |
| Deleted | 27 |
| After | 124 (120 `edge-cases/`, 4 `languages/`), every one `fieldworks_producible: true` |

Counts after the change come from `conformance/generated/hc-conformance-manifest.v1.json`
(`--check-manifest`), not from this table.

## Marked producible without a grammar change

* `edge-cases/final-template-merge-key`: its note named no construct `HCLoader.cs` cannot emit.

## Converted to a producible grammar

Each conversion was re-run against the C# oracle; every remaining expected row is unchanged
except where listed.

* `edge-cases/template-category-sharing`, `edge-cases/final-template-partial-root`,
  `edge-cases/final-template-partial-discriminators`: dropped `outputPartOfSpeech` from slot rules
  (`LoadInflAffixProcessRule` never sets an output part of speech) and, in the two partial
  fixtures, from stratum-level partial rules; removed `partOfSpeech` from the two partial entries
  of `final-template-partial-root` (`LoadLexEntry` sets `IsPartial` only when the MSA has no part
  of speech). No row changed.
* `edge-cases/clitic-rule-designation`, `edge-cases/clitic-flex-layout-blocking-morphology`,
  `edge-cases/non-clitic-after-final-template-layout`: dropped `isCliticRule` (FieldWorks marks a
  clitic only through a stratum named `Clitics`) and the rules and words that depended on the
  non-clitic/clitic contrast, and for the last the Surface-stratum rule. All remaining rows are
  unchanged. These three now pin less than before: the blocking-stratum reset in
  `clitic-flex-layout-blocking-morphology` is no longer observable, and
  `non-clitic-after-final-template-layout` is close to a duplicate of `clitic-rule-designation`.
* `edge-cases/chained-output-feature-override-loss`: `morphologicalRuleOrder` `linear` to
  `unordered` (the loader hardcodes unordered strata). The #504 pin holds: `zudiua` has no parse at
  52d069f8 and parses at 7109531e, for both orders.
* `languages/suffixing-evidential-adjacency-chain`: four `require` co-occurrence rules replaced by
  18 single-other `exclude` rules (the loader builds exclude constraints only; an exclude rule
  with several `otherMorphemes` fires only when all of them co-occur, so "any of" needs one rule
  per morpheme). `siputuku` (first person plural inclusive with no evidential) now parses, because
  an exclude rule cannot refuse the absence of a morpheme. Two negative controls were added,
  `siputukusi` and `sipulupasi`, each shown to parse when the reportative exclude rules are
  removed. The AND-not-OR pin for #311 (`walaknkicha`, blocked by `mrEvidConjectural`) is unchanged.

## Deleted (27)

Grammar needs two character tables, a custom or append-mode MPR feature group, a non-default
`SymbolicFeature.defaultSymbol`, an `isActive="no"` the loader never filters, a
`RealizationalRule`, a family, a discriminating `requiredMPRFeatures` per subrule, or a metathesis
switch order the loader cannot emit. None of these could be converted without losing the
fixture's premise.

`edge-cases/`: bistratal-overlapping-segment-representation, cross-table-root-respelling,
default-symbol-after-merge, feature-gating-breadth, feature-system-breadth, loader-default-symbol,
loader-isactive-breadth, loader-isactive, metathesis-comparison-crash,
mpr-group-overwrite-without-realizational, mpr-overwrite-order-dependence,
multi-table-metathesis-shared-representation, realizational-reduplication-no-proposal-route,
rewrite-analysis-feature-neutralization, rewrite-default-environment, rewrite-default-epenthesis,
rewrite-default-full-set, rewrite-default-narrow, right-to-left-cross-table-segments-environment,
right-to-left-segments-environment, segment-natural-class-table-binding,
synthesis-stratum-render-stale-table, two-table-shared-representation-recall.

`languages/`: fusional-realizational-morphology, prefixal-discontinuous-slot-dependency,
suffixing-extension-slot-ordering, suffixing-vowel-harmony.

## What the suite no longer covers

The deleted fixtures were the only ones to exercise the constructs below. The engine still
implements them; a FieldWorks project cannot reach them through the loader, so the suite no
longer pins them.

* more than one `CharacterDefinitionTable` per grammar, and respelling across a stratum boundary;
* `RealizationalRule` and realizational reduplication;
* custom-named and append-mode `MorphologicalPhonologicalRuleFeatureGroup`;
* `SymbolicFeature.defaultSymbol` and the `rewrite-default-*` behaviour;
* `isActive="no"` on the collection kinds the loader never filters (a `Properties` element, lexical
  entries, allomorphs, strata, feature values);
* `MorphemeCoOccurrenceRule` `type="require"` (the conversion above keeps the exclude form);
* `isCliticRule`.

## Withdrawn follow-up

The earlier analysis proposed converting the family-blocking share of
`languages/suffixing-extension-slot-ordering`. That fixture is deleted, so the proposal is moot.
