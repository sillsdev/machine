# The morphological parser conformance suite

A general-purpose, engine-agnostic conformance test suite for what a morphological/phonological
parser needs to get right, usable independently of this repo and of any one engine.

**The rule: the suite covers only grammars a real FieldWorks project can produce.** FieldWorks never
authors HermitCrab XML; it builds a grammar from a LibLCM project through `HCLoader.cs`, and that
loader reaches less than the XML can say. A grammar that needs something the loader never writes does
not belong here, however expressible it is in the XML and however well the engine handles it. Every
fixture records the verdict as `fieldworks_producible: true` in its `words.yaml`, and
`ConformanceFixtureGateTests` fails on any fixture that does not. The constructs left out, why
FieldWorks cannot write each one, and the 27 fixtures removed for using them are in
[what the XML can say but FieldWorks cannot, so we do not test it](#what-the-xml-can-say-but-fieldworks-cannot-so-we-do-not-test-it).
[`docs/grammars.md`](docs/grammars.md) lists every grammar that remains.

**Surfaces no engine consumes.** Separately from that rule, the XML names eleven surfaces that no engine
reads, so no grammar can make them change a parse. That single fact is recorded in two different
ledgers depending on how a surface fails to earn a counterfactual verdict: `no-consumer` in
`conformance/semantic-coverage-proofs.tsv` for a surface a fixture *does* declare but mutating it
changes nothing, and `dead-schema` in `conformance/semantic-coverage-baseline.txt` for a surface no
fixture ever attempts to declare because the engine never reads the owning element at all (recomputed
by scanning `src/SIL.Machine.Morphology.HermitCrab` for the element name, never trusted from the
ledger). Both are the same underlying claim, zero engine references, so both feed this one list.

<!-- exception-surfaces:start -->
Eleven surfaces across three feature areas are expressible in the DTD and consulted by **no** engine
code path:

- **Cyclic strata and simultaneous rule order** (`no-consumer`): `Stratum/cyclicity="cyclic"` and
  `Stratum/phonologicalRuleOrder="simultaneous"`. `SynthesisStratumRule.Apply` and
  `AnalysisStratumRule.Apply` build the rule cascade unconditionally, with no branch point that could
  select either mode.
- **Syntactic subcategorization** (`dead-schema`, six surfaces): the elements `SyntacticRule`,
  `SyntacticRules`, `OutputSubcategorizationOverride`, `OutputSubcategorizationOverrides`, plus the
  `OutputSubcategorizationOverride/isActive` enum (`yes` and `no`). The DTD wires these through the
  `requiredSubcategorizedRules`, `subcategorizations`, `outputSubcategorization`,
  `headSubcategorizedRules`, and `nonHeadSubcategorizedRules` IDREFS attributes on
  `MorphologicalInput`, `MorphologicalRule`, and `CompoundingRule` — the entire feature is
  unimplemented end to end, not just one element in isolation.
- **Cross-word phonological context** (`dead-schema`, three surfaces): `PreviousWord`, `NextWord`, and
  `Null`, the legal children of every `PhonologicalSubrule` describing an adjacent word's shape.
<!-- exception-surfaces:end -->

A grammar may declare any of these; the engine ignores all eleven. They are unimplemented formalism
features, not coverage gaps. This list is not the list of constructs FieldWorks cannot produce: the
engine reads most of those, and the section named above covers them.
`tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ExceptionListTests.cs` pins this list
against both ledgers so a newly discovered `no-consumer` or `dead-schema` surface cannot silently drop
out of it.

**What passing means today.** An engine reproduces the reference implementation's observable parse
behaviour — signature, status, pinned crash — on every fixture its declared capability profile
admits. "Complete" is currently an *accounting* claim: every inventory item carries an explicit
status, and uncovered means listed rather than unknown. The inventory is the formalism's surfaces,
not languages. Coverage is tracked at three levels: a surface's existence in the DTD (complete by
construction), whether it is load-bearing (evidenced, or classified impossible to evidence with a
reason), and interactions between surfaces (not yet enumerated).

**Open coverage debt is tracked by exact item and resolution** in `semantic-coverage-gap-baseline.tsv`.
These are active gaps, not accepted parity exceptions or formalism exclusions. New or changed gaps
require explicit review and fail the gate; a row is removed when evidence or a valid proof closes its
item. Read the file for the current rows; this page states no count.

**Rule order is part of the contract, not an implementation detail.** A parse runs in two passes:
the word is **torn down** to find candidate stems, then **built back up** to check the rebuild
reproduces the surface it started from. This is analysis-by-synthesis — propose by stripping,
confirm by regenerating — and the engine's own vocabulary is *unapply* for the first pass and
*apply* for the second.

| Pass | Direction | Per-stratum order |
|---|---|---|
| **Analysis** — tearing down (`unapply`) | surface inward | phonological rules unapplied **first**, then affix templates and morphological rules |
| *lexical lookup* | the hinge | each stripped form is looked up for real lexical entries; `Morpher.Synthesize` calls this, so the engine places it at the **start of the build-up** |
| **Synthesis** — building up (`apply`) | stem outward | morphological rules and affix templates first, *mutually recursive* — each may invoke the other — then phonological rules **last** |

Strata run in declaration order building up, and reversed tearing down. Note the two passes are
mirror images: whatever runs last on the way up runs first on the way down.

Two consequences a consumer must reproduce. **Within one stratum pass a phonological rule cannot
feed a morphological rule or template** — phonology runs after morphology in synthesis and before it
in analysis — so that class of interaction does not exist and needs no coverage. **Across strata it
can**: stratum *N*'s phonological output is stratum *N+1*'s morphological input. And because
analysis is the inverse of synthesis, an interaction that *feeds* in one direction *bleeds* in the
other, so an interaction witnessed only in synthesis is untested in the direction a proposer runs.

The order is fixed by the engine, not by the grammar: `SynthesisStratumRule.Apply` and
`AnalysisStratumRule.Apply` build it as an unconditional cascade, which is also why
`Stratum/cyclicity` and `phonologicalRuleOrder` are inert — there is no branch point for them to
select.

`PROTOCOL.md` specifies the adapter contract in engine-agnostic terms;
nothing in `conformance/` depends on Machine's build, layout, or C#. `SIL.Machine.Morphology.HermitCrab`
supplies the seed construct vocabulary (`constructs.txt`) and its C# engine is the **founding
oracle** that generated every fixture's ground truth, but it is not a hard boundary: any engine that
implements `PROTOCOL.md`'s contract can be validated here, declaring a partial **capability
profile** (e.g. morphotactics-only, no phonological rules) to be measured fairly against just the
subset it claims to support.

## Shape

```
conformance/
  languages/<name>/    grammar.xml + words.yaml   -- 4 typologically-selected synthetic languages
  edge-cases/<name>/   grammar.xml + words.yaml   -- focused micro-grammars for things no shared grammar hosts
  docs/grammars.md     one line for every fixture above; a test keeps it complete
  coverage.csv         GENERATED: language x word x construct
  rules.csv            GENERATED: language x grammar rule id x exercising words
  HermitCrabInput.dtd  the published grammar DTD (byte-identical to the library's embedded copy)
  schema/              Draft 2020-12 contracts for the authoring and product formats
  generated/           GENERATED: the versioned JSON manifest a consumer imports
```

## Consuming the suite from another repository

`words.yaml` is the single canonical authored format — the only place case data (inputs, parses,
rejections, skips, `neutralizes`) lives. There is no generated JSON restatement of it: an earlier
`hc-conformance-corpus/v1` product duplicated that ground truth into a 409KB file, and it carried
less than the 383KB of authored `words.yaml`/`grammar.xml` sources it was generated from, because
those sources hold 1011 comment lines of reasoning — why a fixture exists, what it pins — that JSON
cannot express. Two representations of the same ground truth also meant a permanent drift gate. It
has been deleted, along with `schema/conformance-corpus.schema.json`.

What that product genuinely contributed was provenance, not case data, so that is what survives as
`generated/hc-conformance-manifest.v1.json` (`hc-conformance-manifest/v1`, `schema/conformance-manifest.schema.json`,
13KB): one entry per fixture carrying `fixtureId`, `category`, `displayLanguage`, `grammarPath`,
`grammarSha256`, `wordsPath`, `wordsSha256`, `caseCount`, and `expectedCrash`, plus top-level
`dtdPath`, `dtdSha256`, `sourceHash`, and the three format-version identifiers
(`hc-conformance-manifest/v1`, `hc-conformance-words/v1`, `sil-machine-hermit-crab-input-xml/v1`).
A consumer reads the manifest to discover fixtures and verify integrity, then reads `grammar.xml`
and `words.yaml` directly for the actual grammar and cases. Paths inside the manifest are
repository-relative, never relative to the manifest itself.

Regenerate with `hc-conformance --generate-manifest`; `--check-manifest` verifies the checked-in
bytes and exits nonzero on drift. Both validate every grammar against the DTD and every
`words.yaml` against `schema/words.schema.json` before writing anything.

The DTD is published at `conformance/HermitCrabInput.dtd` so a consumer that receives `conformance/`
alone — PanGloss sparse-checks-out exactly that path — can resolve it. The library keeps its own
copy at `src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd` because `XmlLanguageLoader`
reads it as an embedded resource by manifest name; a test holds the two byte-identical. When
resolving a grammar's DOCTYPE, admit only the system identifier `HermitCrabInput.dtd` and map it to
the published DTD path the manifest records. Any other external entity, network fetch, or
filesystem fallback should be refused rather than attempted.

Two files are required per fixture: `grammar.xml` and `words.yaml`. An optional `README.md`
preserves the fixture's rationale and oracle history; optional FieldWorks witnesses are described
in the protocol. `words.yaml` is the single ground truth: front matter (`language`,
`inspired_by`, `sources`, `requires`) plus one entry per word, each carrying a `note` and either
`parses` (`signature`/`gloss`/`rules`/`exercises`, one per distinct analysis), `expect_fail` +
`blocked_by` (well-formed input, zero valid analyses), or `expect_skip` (the oracle throws
`InvalidShapeException`, e.g. an undeclared segment — status "SKIPPED", not "ok"). Optional fields:
`provenance` (bug/PR reference), `guess` (a `LexicalGuess` parse),
`budget_ms`/`expect_crash` (edge-cases only). Signature comparison treats the members of each bracketed
class `[...]` as a set (`|b[ae]t` equals `|b[ea]t`; PROTOCOL.md section 4, sillsdev/machine#573).
Signatures are authored-and-verified, never blindly
regenerated — self-check FAILs on mismatch; `--propose` prints a patch for a human to accept, never
writes. Full schema: `schema/words.schema.json`.

## The four languages

| Directory | Grammar name | Simulates |
|---|---|---|
| `templatic-root-modification` | TemplaticRootModification | Templatic root-and-pattern morphology, stem names, OCP co-occurrence, simultaneous rewrite |
| `metathesis-phase-isolation` | MetathesisPhaseIsolation | Infix, circumfix, reduplication, metathesis, subtractive truncation |
| `polysynthetic-stratal-derivation-chain` | PolysyntheticStratalDerivationChain | Derivation-then-inflection recursion across strata, incorporation-style compounding, guessed stems |
| `suffixing-evidential-adjacency-chain` | SuffixingEvidentialAdjacencyChain | Evidential suffix chain, morpheme-adjacency co-occurrence, `requires: []` — the XAmple-eligible, phonology-free grammar |

Each is a dense, synthetic grammar (invented lexemes and segment inventories, no orthographic claim
about any real language) where ordinary words exercise several constructs at once, rather than a
one-mechanism probe. `edge-cases/` holds the things no shared grammar can host faithfully:
loader/XML-semantics probes, an `expect_crash` pin, a `budget_ms` pathological stress case, and
allomorphy/rewrite pins whose exact rule shape a naturalistic grammar wouldn't contain.

## Truncation identity companions

Three edge cases derive from the published Machine #480 truncation grammar at
`a48b899a456b4d7165cc53e1f76d6ef5dc357dd0` (whose original grammar remains unchanged):
`truncate-morphotactic-identity` adds explicit `MorphemeId` labels; `truncate-morphotactic-subsumption`
uses root `ag` plus prefix rule `X` with the original leading truncation rule `B` and insertion rule
`C`; `truncate-morphotactic-copy-u` adds a copying suffix rule `U` to the explicit-ID A/B/C grammar.
These cases preserve the distinction between a parse's allomorph order in its signature and its
forward rule application order in `words.yaml`'s `rules` list. Repeated signature strings are
intentional and remain repeated rows.

The checked-in expectations were captured from the combined #500+#506 C# source state: Machine #556
commit `d98d12a5d514ce589d7495c71d055830e061a418` plus the Machine #500 `ApplyRhs` relocation at
`157c031ef272d07c900db41b2d4a8852b13f64b0`. The #556 `SIL.Machine.dll` SHA-256 is
`b265a40a44292614f51f7a65afc445ae34dde685cb40ce8c2d2de5521a0c299d`; the combined
`SIL.Machine.Morphology.HermitCrab.dll` SHA-256 is
`4cd980453f51fd3cf29cf38a99f342f542e631eb23be6ba87b9433aab6c3d57f`. Reproduction is: start from
the #480 truncation grammar; add the identity labels or stated companion rule/root; use the #556
Machine library and the #500 relocation in HermitCrab's copy/annotation transfer; parse every
`words.yaml` word; and retain every returned signature and forward trace. These source revisions,
assembly hashes, and the recipe are the provenance; the expected rows do not depend on a temporary
staging file.

The published #480 base lacks both fixes, so its copied annotations do not satisfy these complete
identity rows reliably. Keep the full combined-oracle multisets; do not weaken them to match the
pre-fix base output.

The corrected C# traces report the same analysis features for these rows:
`pos=[posV]`, `syntactic=[POS:v]`, `realizational=ANY`, and the default MPR feature set.
Each root token is its entry's single root XML allomorph, and each rule token is that rule's
single allomorph; every observed allomorph has index 0 and count 1. Signature token order is
the final morph occurrence order, while the tables below retain the separate forward rule
application order. All positive words have status `ok`; `bag` and `bau` have status `ok` with
zero parses, as declared by `expect_fail`. None of the fixture words is skipped.

Stable source attribution is `ROOT` = `root32`/`root32_1` in the identity and copy-U fixtures;
`ROOTAG` = `root32`/`root32_1` in subsumption; `A` = `mruleTruncTrail`, `B` =
`mruleTruncLead`, `C` = `mruleTruncOptIns`, `X` = `mrulePrefixX`, and `U` = `mrU`. The
corresponding `MorphemeId` is explicit in each companion grammar; runtime GUIDs are not used as
fixture identities.

`truncate-morphotactic-identity` expected parses:

| Word | Signature | Forward rule order |
|---|---|---|
| `sag` | `ROOT|sag` | root |
| `sas` | `ROOT33|sas` | root |
| `a` | `ROOT+B+A|a` | B, A |
| `a` | `ROOT+A+B|a` | A, B |
| `ga` | `ROOT+B+C+A|ga` | B, A, C |
| `ga` | `ROOT+A+C+B|ga` | A, B, C |
| `ga` | `C+B+ROOT+A|ga` | B, C, A |
| `ga` | `ROOT+C+A|ga` | A, C |
| `ga` | `C+ROOT+A|ga` | C, A |
| `bag` | no parses | — |

`truncate-morphotactic-subsumption` expected parses:

| Word | Signature | Forward rule order |
|---|---|---|
| `ag` | `ROOTAG|ag` | root |
| `ag` | `ROOTAG+X+B|ag` | X, B |
| `sag` | `X+ROOTAG|sag` | X |
| `gag` | `X+C+ROOTAG|gag` | X, C |
| `gag` | `X+C+ROOTAG+B|gag` | X, B, C |
| `gag` | `X+C+ROOTAG+B|gag` | C, X, B |
| `gag` | `C+ROOTAG|gag` | C |
| `bag` | no parses | — |

`truncate-morphotactic-copy-u` expected parses (the repeated `ROOT+C+A+U|gau` identity is retained):

| Word | Signature | Forward rule order |
|---|---|---|
| `gau` | `ROOT+A+C+B+U|gau` | A, B, C, U |
| `gau` | `ROOT+B+C+A+U|gau` | B, A, C, U |
| `gau` | `B+A+C+ROOT+U|gau` | B, C, A, U |
| `gau` | `C+B+ROOT+A+U|gau` | A, B, U, C |
| `gau` | `C+A+ROOT+B+U|gau` | B, A, U, C |
| `gau` | `ROOT+U+C+A+B|gau` | A, U, B, C |
| `gau` | `ROOT+C+A+U|gau` | A, C, U |
| `gau` | `C+A+ROOT+U|gau` | C, A, U |
| `gau` | `ROOT+C+A+U|gau` | A, U, C |
| `au` | `B+ROOT+A+U|au` | A, B, U |
| `au` | `A+ROOT+B+U|au` | B, A, U |
| `au` | `ROOT+A+U+B|au` | A, U, B |
| `sagu` | `ROOT+U|sagu` | U |
| `bau` | no parses | — |

Every listed allomorph is index 0; the named roots and rules in the signatures are their explicit
`MorphemeId` values. The rules use the existing `AffixProcessRule: subtraction/truncation`
construct label; the fixtures do not introduce new coverage categories. All three declare
`morphologicalRuleOrder="unordered"`, which HCLoader emits. Their words are intentionally synthetic,
but that does not make the grammar constructs FieldWorks-unproducible, so all three correctly carry
`fieldworks_producible: true`.

The combined #500+#506 corrected-engine audit that flips those copied grammars from `unordered` to
`linear` loses analyses in all three companions; the exact changed words and signatures are recorded
in `docs/morphological-rule-order-measurement.md`. The checked-in grammars remain `unordered`,
matching HCLoader's emitted order.

The ordinary conformance runner checks signature multiplicity and the set of rule IDs associated
with each signature. It currently canonicalizes rule IDs before comparing them, so the ordered
`rules` lists preserve the observed application histories but do not independently gate their order.

Fixture and case totals are summed from `generated/hc-conformance-manifest.v1.json`, not
hand-copied here: two prose totals in this file have already drifted from the manifest twice, so
this file no longer states them at all -- read the manifest (`--generate-manifest`/
`--check-manifest`), whose per-fixture `caseCount` is what those flags verify.

## What the XML can say but FieldWorks cannot, so we do not test it

The suite keeps only grammars a real FieldWorks project can produce (see the rule at the top). The
constructs below are valid in `HermitCrabInput.dtd`, and the engine implements them except where a row
says otherwise, but `HCLoader.cs` never writes them. No FieldWorks project can reach them, so a fixture
that needs one would test the engine against input FieldWorks never sends, and the suite does not pin
them. Do not author a fixture for any of them. The `todo` lines in `semantic-coverage-baseline.txt` for
`Family`, `RealizationalRule`, `CharacterDefinitionTable/isActive/no`,
`MorphologicalPhonologicalRuleFeatureGroup/outputType/append` and `isCliticRule` name constructs in this
table; leave them `todo`.

This is a different list from the eleven surfaces above. Those no engine code path reads at all. These the
engine does read, and FieldWorks never writes. Subcategorization (row 17) and `LexicalEntry.morphologicalRules` (row 18) are where the lists meet.

**Where this was checked.** FieldWorks commit `620cd9903f46941ee85947615dba15c8888c8e7a`,
`Src/LexText/ParserCore/HCLoader.cs`, which was last changed by
`6b8a75287221a1b5ab6d15d6f6eedc117690ef98`; LibLCM commit `d564a719b1cce16c25ebea53a537393cb757f5d1`.
The line numbers below are for that file. `fieldworks-producibility.tsv` and its generator were written
against an earlier revision, so their line numbers are stale; their verdicts are unchanged. The
protocol's section 9 holds the rule that applies these to a fixture.

| # | Construct | Why FieldWorks cannot produce it (`HCLoader.cs`) | Deleted fixtures |
|---|---|---|---|
| 1 | More than one `CharacterDefinitionTable`, or a `characterDefinitionTable` on `Segments`/`InsertSegments` (producibility rows `InsertSegments.characterDefinitionTable`, `Segments.characterDefinitionTable`) | `LoadLanguage` calls `LoadCharacterDefinitionTable` once, with `PhonemeSetsOS[0]` (217). Every stratum is built from the one `m_table` (240, 243, 246, `CreateStrata` 387), and `Segments()` (2880) reads it. | 10 |
| 2 | `SymbolicFeature.defaultSymbol` | `LoadFeatureSystem` (2699) reads names and symbols and sets no default; the file never mentions a default symbol. | 6 |
| 3 | `RealizationalRule` | `LoadInflAffixProcessRule` (1025) carries `// TODO: use realizational affix process rules` (1027); the loader builds no realizational rule. | 3 |
| 4 | A custom-named or append-mode `MorphologicalPhonologicalRuleFeatureGroup` | `LoadLanguage` builds exactly three groups: `inflClasses` (181), `exceptionFeatures` (192) and `lexEntryInflTypes` (201). | 4 |
| 5 | A metathesis whose `leftSwitch` names the earlier group (producibility row `MetathesisSwitchPositionInversion`) | `LoadMetathesisRule` (2152) sets `LeftSwitchName = "r"` and `RightSwitchName = "l"` (2171-2172) and names the groups from the structural-change indices (2183-2186). The inverted order is unreachable from the editor: `MetaRuleFormulaControl.GetNextCell` (212-226) orders the cells left environment, left switch, right switch, right environment. LibLCM stores the indices as free text (`GetStrucChangeIndices`, `OverridesLing_Lex.cs:7775`), so this is an editor guarantee, not a data-model one. | 1 |
| 6 | `morphologicalRuleOrder="linear"` that changes a parse (producibility row `morphologicalRuleOrder`) | Every stratum that holds rules is built `Unordered`: `Morphology` (240), `Clitics` (243), each custom stratum (387). The `Surface` stratum (246) keeps the default `Linear` but holds no rule. A `linear` stratum with no listed morphological rule and no active affix template is harmless and stays allowed. | 1 |
| 7 | `isActive="no"` on a kind the loader does not filter, and a second `PhonologicalFeatureSystem` | The loader filters `Disabled` compound rules (254), affix templates (308), phonological rules (315) and ad hoc prohibitions (355, 361), and skips an `IsAbstract` allomorph form (`IsValidRuleForm` 549 at 556, `IsValidLexEntryForm` 592 at 598). It reads no inactive state for features, feature values, natural classes, lexical entries, strata, character tables or `Properties`, and `LoadFeatureSystem` is called once (211). | 3 |
| 8 | `LexicalEntry.family` | The file never reads a family. | 2 |
| 9 | Obligatory output features: `CompoundingRule.outputObligatoryFeatures`, `MorphologicalRule.outputObligatoryFeatures`, `LexicalEntry.obligatoryHeadFeatures`, `LexicalEntry.obligatoryFootFeatures`, and the `ObligatorySyntacticFeatures` failure reason | The file never mentions an obligatory feature. The engine reads `outputObligatoryFeatures` (`XmlLanguageLoader` 898, 1229) but never `obligatoryHeadFeatures` or `obligatoryFootFeatures`, which are engine-dead. | 2 |
| 10 | Head-input MPR features on a compound (`HeadMorphologicalInput.requiredMPRFeatures`, `.excludedMPRFeatures`) and `CompoundingRule.outputProdRestrictionsMprFeatures` | `LoadEndoCompoundingRule` (1891) sets only `OutMprFeatures` (1950) and the head and non-head production restrictions; `LoadExoCompoundingRule` (1971) sets `OutMprFeatures` at 2011 and 2038. Nothing sets head-input MPR features or an output production restriction. | 1 |
| 11 | MPR output (`MPRFeatures`) from a template-slot rule | A rule reached through an `AffixTemplate` slot is inflectional. `LoadInflAffixProcessRule` (1025) sets `RequiredMprFeatures` (1050) but never `OutMprFeatures`; only `LoadDerivAffixProcessRule` (975) does (1018). | 1 |
| 12 | Different `requiredMPRFeatures` on the subrules of one non-inflectional rule | `LoadDerivAffixProcessRule` adds one per-MSA list to every allomorph (1017); `LoadUnclassifiedAffixProcessRule` (1057) adds none. An inflectional rule also adds each allomorph's inflection classes (1118, 1145, 1172), so its subrules may differ. | none |
| 13 | `MorphemeCoOccurrenceRule` or `AllomorphCoOccurrenceRule` with `type="require"` | `LoadAllomorphCoOccurrenceRules` (2212) builds `ConstraintType.Exclude` (2232) and `LoadMorphemeCoOccurrenceRules` (2262) builds it at 2282. | none (one language was rewritten) |
| 14 | `RequiredEnvironments`/`ExcludedEnvironments` on a multi-piece (discontinuous) output | `.Environments.Add` appears only in `LoadRootAllomorph` (870), `LoadCircumfixAffixProcessAllomorph` (1371) and `LoadFormAffixProcessAllomorph` (1626, 1653). `LoadAffixProcessAllomorph` (1383-1488), the path for a multi-action output, never touches environments. | 1 |
| 15 | `isCliticRule` | The loader never sets it. A rule is a clitic only because `LoadCliticAffixProcessRule` (1079) places it in the stratum named `Clitics` (243). | none (three fixtures were rewritten) |
| 16 | A slot rule with `outputPartOfSpeech`; a partial stem entry that has a part of speech; a partial rule inside a slot or with `outputPartOfSpeech` | `LoadInflAffixProcessRule` sets no output part of speech. `IsPartial` is set on an entry only when it has no part of speech (`LoadLexEntry` 757, `LoadLexEntryOfVariant` 817), and on a rule only when it has no slot (1031). `FieldWorksProducibleShapeTests` enforces these. | none (many were rewritten) |
| 17 | Syntactic subcategorization: the nine producibility rows `AffixTemplate.requiredSubcategorizedRules`, `CompoundingRule.headSubcategorizedRules`, `.nonHeadSubcategorizedRules`, `.outputSubcategorization`, `LexicalEntry.subcategorizations`, `MorphologicalRule.outputSubcategorization`, `.requiredSubcategorizedRules` and the two `OutputSubcategorizationOverride` attributes | The file never mentions subcategorization. The engine reads none of it either, so these are also among the eleven surfaces above. | none |
| 18 | `LexicalEntry.morphologicalRules` | Never loaded; the engine does not read it either. | none |

Rows 1 to 6, 8, 9, 10, 17 and 18 cover the 26 `No` rows of `fieldworks-producibility.tsv`
(row 17 holds nine, row 9 five, row 10 three, row 1 two, and rows 2 to 6, 8 and 18 one each). Rows 7 and 11 to 16 are shapes
that no per-attribute row can express: the usage-level and mechanical checks of the protocol's
section 9 and the retirement record's list of what the suite no longer covers. Fixtures were deleted for
rows 1 to 11 and 14; rows 12, 13, 15 and 16 cost no fixture.

The 27 fixtures removed for these constructs, each mapped to the construct that disqualified it, are in
[`docs/engine-only-fixture-retirement.md`](docs/engine-only-fixture-retirement.md).

## What the coverage numbers claim, and what they do not

Every coverage figure here is **per surface** — one DTD element, or one enumerated attribute value.
A surface counts as covered when a fixture exercises it and, in the counterfactual ledger,
neutralizing it changes a real word's parse. That is a strong claim about each surface on its own.

It is **not** a claim about surfaces in combination. Nothing in this suite enumerates or measures
interactions between surfaces, so a green run means "no uncovered surface is known", never "no
uncovered behaviour is known". A defect needing two or more surfaces together can sit behind
fully-covered ingredients indefinitely; one does today. Enumerating every pairwise interaction is
not the fix, since the space is combinatorial while the coverage claim is per-surface; closing this
gap needs selective, named interaction coverage instead, tracked as `conformance/interface-inventory.tsv`
(integration/edge, landed) plus a capped hand-crafted integration/chain set.

## Coverage philosophy

The DTD is the inventory of what a grammar can name; it is not the target. The target is the set of
grammars a FieldWorks project can produce. A DTD surface that only a grammar FieldWorks cannot produce
could exercise stays on the `todo` list of `semantic-coverage-baseline.txt`, and nobody authors a
fixture to close it. The list of those constructs is in
[the section above](#what-the-xml-can-say-but-fieldworks-cannot-so-we-do-not-test-it).

`constructs.txt` is the flat, cross-grammar checklist of grammar-model constructs every fixture's
`exercises:` values are drawn from — the coverage report cross-references against it and flags both
zero-coverage constructs and **dead rules** (a `grammar.xml` rule id no word exercises). A rule named
in an `expect_fail` word's `blocked_by` counts as exercised, but `blocked_by` is **asserted, not
verified**: the report fails when a label is not a rule id in that fixture's grammar, and does not
check the parse trace to confirm the rule blocked the word (tracked in sillsdev/machine#552). `rules:`
per parse is a verified dimension, not aspirational: self-check runs the oracle with tracing on and
FAILs if the traced rule applications diverge from the declared list. Analyses with identical
signatures are retained: self-check compares `(signature, rule-id set)` as a multiset, so two
equal signatures with different valid traces can match independently of result order. A missing,
extra or incorrectly attributed trace still fails. The report prints how many in-scope constructs
are covered (`Tracing (TraceType)` is out of scope — it was never in `expected.tsv`'s domain).

## Running it

```
# self-check: in-process C# oracle against every fixture
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --include-pathological

# adapter mode: materializes words.txt/expected.tsv to temp, runs an external engine
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --adapter "<command template>"

# coverage + dead-rule report: writes coverage.csv and rules.csv
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --coverage-report

# migration parity proof (v1->v2 floors + the permanent absolute construct check)
python conformance/parity-check.py
```

## Growth policy: every bug fix lands with a conformance addition

> **Every bug fix in a conforming engine lands together with a conformance addition** — a new word,
> a new grammar, or a new edge-case.

The cheap path, in order of preference:

1. **Add a word.** One `words.yaml` entry (word + `note` + `parses` + `provenance`) in the language
   whose grammar already covers the territory. No harness or grammar changes.
2. **Extend a grammar** when no existing language's grammar can express the trigger but the
   phenomenon is typologically plausible for one of the four (e.g. a new evidential-chain
   interaction belongs in `suffixing-evidential-adjacency-chain`).
3. **Add an edge-case** when the pin is too specific to embed naturalistically (an XML-loader quirk, a
   crash, an unusual rule shape). It must still be a grammar FieldWorks can produce; a pin that needs
   a construct in [the section on what FieldWorks cannot produce](#what-the-xml-can-say-but-fieldworks-cannot-so-we-do-not-test-it)
   belongs outside this suite.
4. **Add a new `languages/` member** only for genuinely new typological territory not covered by any
   of the four, not for an incremental variant of one that already exists.

## See also

- `PROTOCOL.md` — the adapter contract: CLI shape, TSV format, signature algorithm, capability
  profiles, per-engine grammar representation.
