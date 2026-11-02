# Engine-only fixture retirement: decision record

**Decision (owner).** The conformance suite keeps only grammars a real FieldWorks project can
produce, that is, fixtures marked `fieldworks_producible: true`. A fixture that needs a construct
`HCLoader.cs` (FieldWorks `ParserCore`) cannot emit tests the engine against input FieldWorks never
sends, so it cannot catch a regression a FieldWorks user would see. Such a fixture is converted
to a producible grammar when that preserves its purpose, and deleted when it does not.
`ConformanceFixtureGateTests.EveryFixtureIsMarkedFieldWorksProducible` enforces the rule.

No fixture is kept engine-only. The constructs the suite does not test, and why FieldWorks cannot write
each one, are in the README section
[what the XML can say but FieldWorks cannot, so we do not test it](../README.md#what-the-xml-can-say-but-fieldworks-cannot-so-we-do-not-test-it).
The construct numbers used below are that section's row numbers.

## Result

27 fixtures were deleted. 127 remain (4 `languages/`, 123 `edge-cases/`), every one
`fieldworks_producible: true`. The count comes from `conformance/generated/hc-conformance-manifest.v1.json`
(`--check-manifest`), not from this page.

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
* The #612 pass over fixtures already marked `true` (no row changed except where listed):
  * Seven fixtures whose `linear` stratum lists morphological rules or holds an affix template now
    say `unordered`, because the engine reads the order for both (`compounding-breadth`,
    `head-ambiguous-compounding`, `recursive-endocentric-compounding`,
    `subrule-morphosyntactic-gating`, `guesser-pattern-root-fallback`,
    `pattern-root-required-environment`, `standalone-combining-mark`); the oracle found no change
    in any word. `morphological-rule-order-measurement.md` has the count.
  * 18 fixtures dropped `outputPartOfSpeech` from every slot rule. No row changed.
  * `edge-cases/morphotactic-attribute-breadth`: the partial stem entry `eNap` lost its part of
    speech, the partial slot rule `mrPartial` became the ordinary suffix `mrMo` (so `kulmo`'s
    signature is `KUL+MO|kulmo`), and the partial stratum rule `mrPO` lost its
    `outputPartOfSpeech`. `napmo` and `pogliti` still pin the two partial flags.
  * `FieldWorksProducibleShapeTests` fails when a `true` fixture brings any of these shapes back.

## Deleted (27)

None could be converted to a producible grammar without losing its premise. The first column is the
fixture name; the second is the construct (README row numbers) that made it unproducible, with what the
grammar used; the third is what it tested. A fixture may need more than one construct.

| Fixture | Construct (README row) | What it tested |
|---|---|---|
| `edge-cases/bistratal-overlapping-segment-representation` | 1: two character tables, one per stratum | whether a segment spelled differently on two strata is accepted or refused when each stratum has its own lexicon (the refusal side of shared representation) |
| `edge-cases/cross-table-root-respelling` | 1: tables `tIn` and `tFin` | a root entered on an inner stratum surfaces spelled by the final table's segment with the same feature bundle, with no phonological rule |
| `edge-cases/default-symbol-after-merge` | 2: a `defaultSymbol` | default-symbol matching on a segment produced by a merge rule |
| `edge-cases/feature-gating-breadth` | 6: three words (`kalidka`, `kalnoka`, `kalidmu`) that depend on rule-list order in a `linear` stratum | head and foot feature gating in the smallest grammar that discriminates it |
| `edge-cases/feature-system-breadth` | 7: `isActive="no"` on a symbolic feature, a complex feature, a feature value, both natural class kinds and a lexical entry | deactivation of feature-system elements, complex features, right-to-left metathesis and an alpha variable |
| `edge-cases/loader-default-symbol` | 2: a `defaultSymbol` | an under-specified segment counts as the default value (nasal +), so a rule that targets oral segments must not apply to it |
| `edge-cases/loader-isactive-breadth` | 1, 7, 8: three character tables, `isActive="no"` on more than a dozen kinds, `family` twice | an inactive decoy beside the live declaration for each collection the XML loader filters |
| `edge-cases/loader-isactive` | 7: a second, inactive `PhonologicalFeatureSystem` block | the loader uses only the active feature-system block |
| `edge-cases/metathesis-comparison-crash` | 5: a `leftSwitch` that names the earlier group | the engine's pinned failure when the left switch is the earlier of the two groups |
| `edge-cases/mpr-group-overwrite-without-realizational` | 4: a custom-named group (`ThemeGroup`) | an overwrite-mode MPR group in a grammar with no realizational rule |
| `edge-cases/mpr-overwrite-order-dependence` | 4: an overwrite group and an append group | on an unordered stratum, an overwrite group keeps only what the last rule wrote, so the result depends on application order |
| `edge-cases/multi-table-metathesis-shared-representation` | 1: tables `t0` and `t1` | metathesis across two tables that share a representation |
| `edge-cases/realizational-reduplication-no-proposal-route` | 3: a `RealizationalRule` | reduplication driven by a realizational rule |
| `edge-cases/rewrite-analysis-feature-neutralization` | 1: tables `tCore` and `tSurf` | cross-table respelling together with analysis-side feature neutralization |
| `edge-cases/rewrite-default-environment` | 2: a `defaultSymbol` | default-symbol matching in a rule's environment |
| `edge-cases/rewrite-default-epenthesis` | 2: a `defaultSymbol` | default-symbol matching around an epenthesis rule |
| `edge-cases/rewrite-default-full-set` | 2: a `defaultSymbol` | default-symbol matching when a rule names the full feature set |
| `edge-cases/rewrite-default-narrow` | 2: a `defaultSymbol` | default-symbol matching in a narrowing rule |
| `edge-cases/right-to-left-cross-table-segments-environment` | 1: two tables and a `characterDefinitionTable` on `Segments` | a right-to-left rewrite whose right environment is an inline `Segments` literal spelled in the second table |
| `edge-cases/right-to-left-segments-environment` | 1: a `characterDefinitionTable` on `Segments`, with one table | a right-to-left iterative rewrite whose right environment is an inline `Segments` literal |
| `edge-cases/segment-natural-class-table-binding` | 1: tables `t0` and `t1`, one per stratum, with a natural class over segments | a natural class over segments binds to its own stratum's table |
| `edge-cases/synthesis-stratum-render-stale-table` | 1: tables `tRoot` and `tFace` | the parse signature is spelled with a stratum's table that synthesis carried forward |
| `edge-cases/two-table-shared-representation-recall` | 1: tables `t0` and `t1`, one per stratum | the recall side of shared representation: material threaded from one table to the other through a rule |
| `languages/fusional-realizational-morphology` | 3, 4, 9, 10: `RealizationalRule` `rrPast`, three custom MPR groups, `outputObligatoryFeatures` on a compound, required and excluded MPR features on the compound head (`mrCompoundConstrained`) | a fusional language with realizational tense, ablaut, stem names and constrained compounding |
| `languages/prefixal-discontinuous-slot-dependency` | 11: MPR output from the slot rule `mrModeTrans`, required by a later slot | a prefixing language where a later slot depends on an earlier, non-adjacent one |
| `languages/suffixing-extension-slot-ordering` | 3, 4, 8, 9: two `RealizationalRule`s, four custom MPR groups, `family` on `eZam` and `eWep`, `outputObligatoryFeatures` on `mrNumPl` | verbal extension suffixes in a fixed slot order, with nasal mutation |
| `languages/suffixing-vowel-harmony` | 14: `RequiredEnvironments` on the discontinuous rule `ruleCollective` | a suffixing language with alpha-variable vowel harmony, gradation and merges |

Counts by row: 1 has 10 fixtures, 2 has 6, 3 has 3, 4 has 4, 5 has 1, 6 has 1, 7 has 3, 8 has 2, 9 has 2,
10 has 1, 11 has 1, 14 has 1.

## Retirement doubts

Each fixture below might have been kept after a conversion that loses little. None is restored here. The
conversions were tried on scratch copies and checked with the founding oracle.

* `edge-cases/feature-gating-breadth`. Setting its stratum to `unordered` and marking it producible
  changes three of 15 words (`kalidka`, `kalnoka`, `kalidmu`), all `expect_fail` rows that tested only
  rule-list order. Dropping those three rows leaves the self-check passing, and the other 12 words
  keep its head and foot feature gating (`tunka`, `molka`, `nalnomu`). Strong doubt: the fixture's
  purpose survives.
* `languages/suffixing-vowel-harmony`. Dropping the discontinuous rule `ruleCollective`, its stratum
  reference, the two words only it blocked (`etemik`, `esalik`) and `outputPartOfSpeech` on the three
  slot rules passes with 25 of 27 words. No positive word used `ruleCollective`. Strong doubt.
* `languages/suffixing-extension-slot-ordering`. Without its `RealizationalRule`s, MPR groups, `family`
  attributes and `outputObligatoryFeatures`, 48 of 56 words pass unchanged; eight differ (`nbael`,
  `npael`, `limaile`, `yxkibz`, `belh`, `sekwz`, `sekwh`, `dalehiz`) because they depend on the removed
  constructs. Partial doubt. Its recorded reason that a slot rule writes an MPR feature does not hold:
  `mrCaus`, `mrAppl` and `mrFV` carry no `MPRFeatures`.
* `languages/fusional-realizational-morphology`. The same removals, plus `mrCompoundConstrained`, change
  five of 62 words (`ferid`, `ygofz`, `yxpedz`, `sodh`, `gofwh`); the realizational point is lost.
  Partial doubt.
* `edge-cases/right-to-left-segments-environment`. Its own note says the five words are reachable
  through `IPhSimpleContextSeg`; only the PanGloss lowering of `Segments` is lost. Not run. Mild doubt.

Claims in the deleted fixtures' notes that do not hold at FieldWorks `620cd990`:

* `edge-cases/loader-isactive-breadth` says an allomorph has no inactive state. A form with `IsAbstract`
  is skipped (`IsValidRuleForm`, `IsValidLexEntryForm`). The fixture still needs three tables and `family`.
* `languages/prefixal-discontinuous-slot-dependency` says one required-MPR list applies to every subrule.
  That is true of a derivational rule; `mrSubj` sits in a slot, where subrules may differ. Its other
  reason, MPR output from a slot rule, holds.
* No deleted fixture used differing `requiredMPRFeatures` across the subrules of a non-inflectional rule.
  Row 12 is a shape the suite avoids; it is not a reason any fixture was deleted.
* `edge-cases/metathesis-comparison-crash` stays out, but the switch order is an editor guarantee
  (`MetaRuleFormulaControl.GetNextCell`), not a LibLCM data-model one.
