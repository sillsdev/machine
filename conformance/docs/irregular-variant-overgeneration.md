# Substitute-mechanism over-generation probe (synthetic)

Tests the claim: FieldWorks' HCLoader.cs substitute for "irregularly inflected forms" (MPR-feature
tagging + `ExcludedMprFeatures` + a synthesized null-affix rule per slot, HCLoader.cs:626-807 and
1702-1804) blocks the IRREGULAR entry from double-marking with the regular affix, but does NOT stop
the REGULAR root from independently combining with the regular affix.

Both fixtures are hand-built raw HermitCrab XML (`HermitCrabInput.dtd` shape), constructed to mirror
byte-for-byte the object shape `HCLoader.cs` is documented (by direct code trace) to emit for this
scenario -- NOT produced by projecting a real FieldWorks/LibLCM project, because the `xample-projector`
`author` tool's supported grammar.xml subset (see its README's "author's supported subset" table) has
no way to construct FieldWorks' `LexEntryRef`/`ILexEntryInflType` "irregularly inflected form" variant
mechanism at all -- that construct is purely a LibLCM/UI concept with no representation in the
HermitCrab-native `grammar.xml`/DTD format the author tool consumes. This hand-built XML therefore
tests the HC ENGINE's behavior under the exact substitute construction, not the LibLCM-to-HC
projection step itself -- the projection step was instead verified by direct code reading of
HCLoader.cs (see the parent report).

All English words below (go/went/goed/wented) are private shorthand for the synthetic p/q/s
morphemes actually used in the grammars -- nothing here models a real language.

## How to run

```powershell
$oracle = "C:\Users\johnm\Documents\repos\machine\src\SIL.Machine.Morphology.HermitCrab.Conformance\bin\Release\net10.0\hc-conformance.exe"
& $oracle --fixtures ".\base"   # edge-cases/substitute-mpr-probe
& $oracle --fixtures ".\adhoc"  # edge-cases/substitute-mpr-probe-adhoc
```

Every word in both `words.yaml` files carries a deliberately bogus expected parse
(`signature: "BOGUS|bogus"`), so every word always reports a mismatch and the harness's own
`Runner.cs` mismatch-detail branch prints the REAL analyses it found (`"expected [...] got [...]"`).
This is a one-shot probe meant to be read for its printed `got [...]` detail, not a fixture meant to
stay green.

## `base` (edge-cases/substitute-mpr-probe): the substitute alone

- eP = regular root (analogue of "go"), no MPR feature, ordinary `LoadLexEntry`.
- eQ = irregular variant entry (analogue of "went"), carries MPR feature `mFeatIrr`
  (analogue of `HCLoader.cs:746`'s `hcEntry.MprFeatures.Add(m_mprFeatures[inflType])`).
- mrSFX = the regular affix (analogue of "-ed"), its subrule EXCLUDES `mFeatIrr`
  (analogue of `HCLoader.cs:1716-1717`'s `allo.ExcludedMprFeatures.Add(...)`).
- mrNull = the synthesized null-affix rule, REQUIRES `mFeatIrr`
  (analogue of `LoadNullAffixProcessRule`, `HCLoader.cs:1771-1806`).
- One obligatory affix-template slot holding both mrSFX and mrNull.

**Measured result** (`hc-conformance.exe --fixtures .\base`, self-check mode = the C# founding oracle
`Morpher`):

| word | analogue | actual result |
|---|---|---|
| `p` | bare "go" | `[]` -- 0 parses (slot obligatory, correctly rejected) |
| `ps` | **"goed"** | **`[P+SFX\|ps]` -- 1 parse, ACCEPTED** |
| `q` | "went" | `[\|q]` -- 1 parse (via the null-affix rule) |
| `qs` | "wented" (double marking) | `[]` -- 0 parses, correctly BLOCKED |

`ps` parsing is the over-generation: the regular root, which never carries the MPR feature, combines
freely with the regular affix even though the irregular entry `eQ` already covers this cell -- exactly
the "goed" scenario the claim under test describes. The substitute construction blocks only the
half it was ever wired to block (`qs`).

## `adhoc` (edge-cases/substitute-mpr-probe-adhoc): + one author-added ad hoc prohibition

Identical grammar, plus one `MorphemeCoOccurrenceRule type="exclude" primaryMorpheme="eP"
otherMorphemes="mrSFX"` (the raw-XML form of `IMoMorphAdhocProhib`, the ad hoc co-occurrence rule
LibLCM's own model comment says exists for "co-occurrence restrictions ... which cannot be captured
using morphosyntactic or phonological restrictions" -- `liblcm/src/SIL.LCModel/MasterLCModel.xml`
around line 2966 -- projected by `HCLoader.LoadMorphemeCoOccurrenceRules`, `HCLoader.cs:2213-2239`).

**Measured result:**

| word | actual result |
|---|---|
| `p` | `[]` |
| `ps` | **`[]` -- now BLOCKED (was 1 parse in `base`)** |
| `q` | `[NULL+Q\|q]` -- still 1 parse, unaffected |
| `qs` | `[]` -- still blocked |

Adding the ad hoc prohibition between the regular root and the regular affix suppresses the
over-generation with no effect on the irregular entry's own parse. This is the "does not apply"
mechanism the requester predicted: FieldWorks models it (`MoMorphAdhocProhib`/`MoAlloAdhocProhib`,
LibLCM), exposes it in the real UI (`DistFiles/Language Explorer/Configuration/Parts/
MorphologyParts.xml` around line 2455, "Key Morpheme"/"Other Morpheme(s)"/"Cannot Occur"), and
`HCLoader.cs` projects it faithfully -- it is simply a SEPARATE, author-driven mechanism from the
automatic "irregularly inflected form" substitute, and the automatic substitute does not invoke it on
its own.

## The two grammars, inlined

They are inlined rather than committed as files because a repo gate --
`EveryGrammarTheCoverageGateReadsBelongsToADiscoveredFixture` -- correctly refuses any
`conformance/**/grammar.xml` that is not a discovered fixture, and these are probes, not fixtures.
To run: write each pair into `<dir>/edge-cases/<name>/{grammar.xml,words.yaml}` and point
`hc-conformance.exe --fixtures <dir>` at it.

### base -- substitute-mpr-probe

```xml
<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
<HermitCrabInput>
  <Language>
    <Name>SubstituteMprProbe</Name>
    <!--
      Synthetic probe (not modeling any real language) that hand reconstructs, at the raw HC XML
      level, EXACTLY the object shape FieldWorks' HCLoader.cs builds for an "irregularly inflected
      form" via its documented substitute (HCLoader.cs lines 626 through 807 and 1702 through 1804):
      a synthesized MPR "feature" tagging the irregular variant's LexEntry (mirrors
      LoadLexEntryOfVariant's hcEntry.MprFeatures.Add(m_mprFeatures[inflType]) at HCLoader.cs:746),
      the regular slot rule's allomorph carrying ExcludedMprFeatures for that same MPR feature
      (mirrors LoadAffixTemplate's allo.ExcludedMprFeatures.Add(m_mprFeatures[t]) around
      HCLoader.cs:1716), and a synthesized null-affix rule requiring that MPR feature to admit the
      irregular form bare in the same obligatory slot (mirrors LoadNullAffixProcessRule around
      HCLoader.cs:1771 through 1806, RequiredMprFeatures set around :1783). Tests whether this
      construction, on its own, blocks a SEPARATE regular entry (which never carries the MPR
      feature) from combining with the regular affix, which is the claim under test. eP, eQ,
      mrSFX, mrNull are placeholder morpheme ids, not modeling any language.
    -->
    <PartsOfSpeech>
      <PartOfSpeech id="posV"><Name>v</Name></PartOfSpeech>
    </PartsOfSpeech>
    <MorphologicalPhonologicalRuleFeatures>
      <MorphologicalPhonologicalRuleFeature id="mFeatIrr">Irr</MorphologicalPhonologicalRuleFeature>
    </MorphologicalPhonologicalRuleFeatures>
    <CharacterDefinitionTable id="t1">
      <Name>Main</Name>
      <SegmentDefinitions>
        <SegmentDefinition id="cP"><Representations><Representation>p</Representation></Representations></SegmentDefinition>
        <SegmentDefinition id="cQ"><Representations><Representation>q</Representation></Representations></SegmentDefinition>
        <SegmentDefinition id="cS"><Representations><Representation>s</Representation></Representations></SegmentDefinition>
      </SegmentDefinitions>
    </CharacterDefinitionTable>
    <NaturalClasses>
      <FeatureNaturalClass id="ncAny"><Name>Any</Name></FeatureNaturalClass>
    </NaturalClasses>
    <Strata>
      <Stratum characterDefinitionTable="t1" morphologicalRuleOrder="unordered">
        <Name>Main</Name>
        <MorphologicalRuleDefinitions>
          <!-- mrSFX = the REGULAR productive affix (analogue of English "-ed"). Its subrule excludes
               the "Irr" MPR feature, exactly as HCLoader.cs:1716-1717 does for a regular slot rule
               once an ILexEntryInflType is found on the slot. -->
          <MorphologicalRule id="mrSFX" requiredPartsOfSpeech="posV" outputPartOfSpeech="posV">
            <Name>sfx</Name>
            <MorphologicalSubrules>
              <MorphologicalSubrule id="subSFX">
                <MorphologicalInput excludedMPRFeatures="mFeatIrr">
                  <PhoneticSequence id="stemSFX"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence>
                </MorphologicalInput>
                <MorphologicalOutput><CopyFromInput index="stemSFX" /><InsertSegments><PhoneticShape>s</PhoneticShape></InsertSegments></MorphologicalOutput>
              </MorphologicalSubrule>
            </MorphologicalSubrules>
            <MorphemeId>SFX</MorphemeId>
          </MorphologicalRule>
          <!-- mrNull = HCLoader's synthesized null-affix rule (LoadNullAffixProcessRule), required
               so the irregular variant can still fill the obligatory slot. RequiredMprFeatures ties
               it to the SAME "Irr" feature. -->
          <MorphologicalRule id="mrNull" requiredPartsOfSpeech="posV" outputPartOfSpeech="posV">
            <Name>null</Name>
            <MorphologicalSubrules>
              <MorphologicalSubrule id="subNull">
                <MorphologicalInput requiredMPRFeatures="mFeatIrr">
                  <PhoneticSequence id="stemNull"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence>
                </MorphologicalInput>
                <MorphologicalOutput><CopyFromInput index="stemNull" /></MorphologicalOutput>
              </MorphologicalSubrule>
            </MorphologicalSubrules>
            <MorphemeId>NULL</MorphemeId>
          </MorphologicalRule>
        </MorphologicalRuleDefinitions>
        <AffixTemplates>
          <!-- One OBLIGATORY slot (optional="false"), matching HCLoader.cs:1727's
               `!slot.Optional` gate for adding the null-affix rule. -->
          <AffixTemplate requiredPartsOfSpeech="posV">
            <Name>probeTemplate</Name>
            <Slot optional="false" morphologicalRules="mrSFX mrNull">
              <Name>slot1</Name>
            </Slot>
          </AffixTemplate>
        </AffixTemplates>
        <LexicalEntries>
          <!-- eP = the REGULAR root (analogue of "go"). Loaded the ordinary way (HCLoader's
               LoadLexEntry), so it carries NO MPR feature at all. -->
          <LexicalEntry id="eP" partOfSpeech="posV">
            <Allomorphs><Allomorph id="aP"><PhoneticShape>p</PhoneticShape></Allomorph></Allomorphs>
            <MorphemeId>P</MorphemeId>
          </LexicalEntry>
          <!-- eQ = the irregular variant entry (analogue of "went"), loaded via HCLoader's
               LoadLexEntryOfVariant, which is the ONLY place that adds the MPR feature
               (HCLoader.cs:746). -->
          <LexicalEntry id="eQ" partOfSpeech="posV" ruleFeatures="mFeatIrr">
            <Allomorphs><Allomorph id="aQ"><PhoneticShape>q</PhoneticShape></Allomorph></Allomorphs>
            <MorphemeId>Q</MorphemeId>
          </LexicalEntry>
        </LexicalEntries>
      </Stratum>
    </Strata>
  </Language>
</HermitCrabInput>
```

```yaml
language: SubstituteMprProbe
inspired_by: ["synthetic probe of FieldWorks HCLoader's irregular-inflection substitute mechanism (not modeling any real language)"]
sources:
  - "Src/LexText/ParserCore/HCLoader.cs:626-807,1702-1804 (LoadLexEntryOfVariant, LoadAffixTemplate, LoadNullAffixProcessRule)"
requires: []
fieldworks_producible: true
words:
  # Deliberately bogus expected parses on every word below (signature "BOGUS|bogus") so the
  # self-check harness reports a mismatch, and therefore prints the REAL actual signature(s) for
  # every word, regardless of what was predicted -- see Runner.cs's mismatch-detail branch. This is
  # a one-shot probe read for its printed "got [...]" detail, not a fixture meant to stay green.
  - word: "p"
    note: "Regular root alone (analogue of bare 'go'). Slot is obligatory, so this should have 0 parses (mrSFX not excluded but not required either... mrNull requires the Irr feature p lacks). Predicting 0."
    parses:
      - signature: "BOGUS|bogus"
        rules: []

  - word: "ps"
    note: "Regular root + regular affix (analogue of 'goed'). Nothing on eP excludes mrSFX, so under the claim this SHOULD parse -- the over-generation under test."
    parses:
      - signature: "BOGUS|bogus"
        rules: []

  - word: "q"
    note: "Irregular variant alone (analogue of 'went'), parsed via the synthesized null-affix rule mrNull (requires the Irr feature eQ carries). Predicting 1 parse."
    parses:
      - signature: "BOGUS|bogus"
        rules: []

  - word: "qs"
    note: "Irregular variant + regular affix (analogue of 'wented', double-marking). mrSFX excludes the Irr feature eQ carries, so this should be the one case the substitute DOES correctly block. Predicting 0 parses."
    parses:
      - signature: "BOGUS|bogus"
        rules: []
```

### adhoc -- substitute-mpr-probe-adhoc

```xml
<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
<HermitCrabInput>
  <Language>
    <Name>SubstituteMprProbeAdhoc</Name>
    <!--
      Same construction as edge-cases/substitute-mpr-probe, PLUS one author-added
      MorphemeCoOccurrenceRule (HCLoader.cs:341-350/2213-2239, IMoMorphAdhocProhib in LibLCM,
      MasterLCModel.xml around line 2964) that explicitly excludes the REGULAR root eP from
      co-occurring with the REGULAR affix mrSFX. This is the "does not apply" mechanism a grammar
      author could add by hand to close the gap the base probe measures. eP, eQ, mrSFX, mrNull are
      placeholder morpheme ids, not modeling any language.
    -->
    <PartsOfSpeech>
      <PartOfSpeech id="posV"><Name>v</Name></PartOfSpeech>
    </PartsOfSpeech>
    <MorphologicalPhonologicalRuleFeatures>
      <MorphologicalPhonologicalRuleFeature id="mFeatIrr">Irr</MorphologicalPhonologicalRuleFeature>
    </MorphologicalPhonologicalRuleFeatures>
    <CharacterDefinitionTable id="t1">
      <Name>Main</Name>
      <SegmentDefinitions>
        <SegmentDefinition id="cP"><Representations><Representation>p</Representation></Representations></SegmentDefinition>
        <SegmentDefinition id="cQ"><Representations><Representation>q</Representation></Representations></SegmentDefinition>
        <SegmentDefinition id="cS"><Representations><Representation>s</Representation></Representations></SegmentDefinition>
      </SegmentDefinitions>
    </CharacterDefinitionTable>
    <NaturalClasses>
      <FeatureNaturalClass id="ncAny"><Name>Any</Name></FeatureNaturalClass>
    </NaturalClasses>
    <Strata>
      <Stratum characterDefinitionTable="t1" morphologicalRuleOrder="unordered">
        <Name>Main</Name>
        <MorphologicalRuleDefinitions>
          <MorphologicalRule id="mrSFX" requiredPartsOfSpeech="posV" outputPartOfSpeech="posV">
            <Name>sfx</Name>
            <MorphologicalSubrules>
              <MorphologicalSubrule id="subSFX">
                <MorphologicalInput excludedMPRFeatures="mFeatIrr">
                  <PhoneticSequence id="stemSFX"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence>
                </MorphologicalInput>
                <MorphologicalOutput><CopyFromInput index="stemSFX" /><InsertSegments><PhoneticShape>s</PhoneticShape></InsertSegments></MorphologicalOutput>
              </MorphologicalSubrule>
            </MorphologicalSubrules>
            <MorphemeId>SFX</MorphemeId>
          </MorphologicalRule>
          <MorphologicalRule id="mrNull" requiredPartsOfSpeech="posV" outputPartOfSpeech="posV">
            <Name>null</Name>
            <MorphologicalSubrules>
              <MorphologicalSubrule id="subNull">
                <MorphologicalInput requiredMPRFeatures="mFeatIrr">
                  <PhoneticSequence id="stemNull"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence>
                </MorphologicalInput>
                <MorphologicalOutput><CopyFromInput index="stemNull" /></MorphologicalOutput>
              </MorphologicalSubrule>
            </MorphologicalSubrules>
            <MorphemeId>NULL</MorphemeId>
          </MorphologicalRule>
        </MorphologicalRuleDefinitions>
        <AffixTemplates>
          <AffixTemplate requiredPartsOfSpeech="posV">
            <Name>probeTemplate</Name>
            <Slot optional="false" morphologicalRules="mrSFX mrNull">
              <Name>slot1</Name>
            </Slot>
          </AffixTemplate>
        </AffixTemplates>
        <LexicalEntries>
          <LexicalEntry id="eP" partOfSpeech="posV">
            <Allomorphs><Allomorph id="aP"><PhoneticShape>p</PhoneticShape></Allomorph></Allomorphs>
            <MorphemeId>P</MorphemeId>
          </LexicalEntry>
          <LexicalEntry id="eQ" partOfSpeech="posV" ruleFeatures="mFeatIrr">
            <Allomorphs><Allomorph id="aQ"><PhoneticShape>q</PhoneticShape></Allomorph></Allomorphs>
            <MorphemeId>Q</MorphemeId>
          </LexicalEntry>
        </LexicalEntries>
      </Stratum>
    </Strata>
    <MorphemeCoOccurrenceRules>
      <MorphemeCoOccurrenceRule type="exclude" primaryMorpheme="eP" otherMorphemes="mrSFX" adjacency="anywhere" />
    </MorphemeCoOccurrenceRules>
  </Language>
</HermitCrabInput>
```

```yaml
language: SubstituteMprProbeAdhoc
inspired_by: ["synthetic probe of an author-added ad hoc co-occurrence prohibition closing the substitute-mechanism gap (not modeling any real language)"]
sources:
  - "Src/LexText/ParserCore/HCLoader.cs:341-350,2213-2239 (LoadMorphemeCoOccurrenceRules, IMoMorphAdhocProhib projection)"
requires: []
fieldworks_producible: true
words:
  # Same deliberately-bogus-expected-parses probe technique as edge-cases/substitute-mpr-probe: every
  # word below is expected to mismatch so the harness prints the real "got [...]" signature.
  - word: "p"
    note: "Regular root alone. Predicting 0 parses (slot obligatory, same as the base probe)."
    parses:
      - signature: "BOGUS|bogus"
        rules: []

  - word: "ps"
    note: "Regular root + regular affix. With the ad hoc MorphemeCoOccurrenceRule excluding eP from mrSFX, this should now be BLOCKED (0 parses) -- the fix under test."
    parses:
      - signature: "BOGUS|bogus"
        rules: []

  - word: "q"
    note: "Irregular variant alone, via the null-affix rule. Unaffected by the new ad hoc rule (which only names eP/mrSFX). Predicting 1 parse, same as the base probe."
    parses:
      - signature: "BOGUS|bogus"
        rules: []

  - word: "qs"
    note: "Irregular variant + regular affix (double marking). Still blocked by the pre-existing ExcludedMprFeatures mechanism, same as the base probe. Predicting 0 parses."
    parses:
      - signature: "BOGUS|bogus"
        rules: []
```
