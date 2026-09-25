using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Regression coverage for a dropped morpheme ID when a zero-width rule wraps one ending at the
/// same shape node.
/// </summary>
[TestFixture]
public class IdentityRuleSignatureMorphDropTests
{
    private const string GrammarXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
        <HermitCrabInput>
          <Language>
            <Name>IdentityRuleSignatureMorphDropTest</Name>
            <PartsOfSpeech>
              <PartOfSpeech id="posX"><Name>x</Name></PartOfSpeech>
            </PartsOfSpeech>
            <HeadFeatures>
              <SymbolicFeature id="featGrp">
                <Name>grp</Name>
                <Symbols>
                  <Symbol id="symP">p</Symbol>
                  <Symbol id="symQ">q</Symbol>
                </Symbols>
              </SymbolicFeature>
            </HeadFeatures>
            <CharacterDefinitionTable id="tbl">
              <Name>Main</Name>
              <SegmentDefinitions>
                <SegmentDefinition id="cF"><Representations><Representation>f</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cE"><Representations><Representation>e</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cN"><Representations><Representation>n</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cO"><Representations><Representation>o</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cS"><Representations><Representation>s</Representation></Representations></SegmentDefinition>
              </SegmentDefinitions>
            </CharacterDefinitionTable>
            <NaturalClasses>
              <FeatureNaturalClass id="ncAny"><Name>Any</Name></FeatureNaturalClass>
            </NaturalClasses>
            <Strata>
              <Stratum characterDefinitionTable="tbl" morphologicalRules="mrPtoQ mrQtoP mrIdentity">
                <Name>Main</Name>
                <MorphologicalRuleDefinitions>
                  <MorphologicalRule id="mrPtoQ">
                    <Name>ptoq</Name>
                    <MorphologicalSubrules>
                      <MorphologicalSubrule id="subPtoQ">
                        <MorphologicalInput><PhoneticSequence id="stemPtoQ"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence></MorphologicalInput>
                        <MorphologicalOutput><CopyFromInput index="stemPtoQ" /><InsertSegments><PhoneticShape>o</PhoneticShape></InsertSegments></MorphologicalOutput>
                      </MorphologicalSubrule>
                    </MorphologicalSubrules>
                    <OutputHeadFeatures><FeatureValue feature="featGrp" symbolValues="symQ" /></OutputHeadFeatures>
                    <RequiredHeadFeatures><FeatureValue feature="featGrp" symbolValues="symP" /></RequiredHeadFeatures>
                    <MorphemeId>PTOQ</MorphemeId>
                    <Gloss>PTOQ</Gloss>
                  </MorphologicalRule>
                  <MorphologicalRule id="mrQtoP">
                    <Name>qtop</Name>
                    <MorphologicalSubrules>
                      <MorphologicalSubrule id="subQtoP">
                        <MorphologicalInput><PhoneticSequence id="stemQtoP"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence></MorphologicalInput>
                        <MorphologicalOutput><CopyFromInput index="stemQtoP" /><InsertSegments><PhoneticShape>s</PhoneticShape></InsertSegments></MorphologicalOutput>
                      </MorphologicalSubrule>
                    </MorphologicalSubrules>
                    <OutputHeadFeatures><FeatureValue feature="featGrp" symbolValues="symP" /></OutputHeadFeatures>
                    <RequiredHeadFeatures><FeatureValue feature="featGrp" symbolValues="symQ" /></RequiredHeadFeatures>
                    <MorphemeId>QTOP</MorphemeId>
                    <Gloss>QTOP</Gloss>
                  </MorphologicalRule>
                  <MorphologicalRule id="mrIdentity">
                    <Name>identity</Name>
                    <MorphologicalSubrules>
                      <MorphologicalSubrule id="subIdentity">
                        <MorphologicalInput><PhoneticSequence id="stemIdentity"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence></MorphologicalInput>
                        <MorphologicalOutput><CopyFromInput index="stemIdentity" /></MorphologicalOutput>
                      </MorphologicalSubrule>
                    </MorphologicalSubrules>
                    <OutputHeadFeatures><FeatureValue feature="featGrp" symbolValues="symQ" /></OutputHeadFeatures>
                    <RequiredHeadFeatures><FeatureValue feature="featGrp" symbolValues="symP" /></RequiredHeadFeatures>
                    <MorphemeId>IDENT</MorphemeId>
                    <Gloss>IDENT</Gloss>
                  </MorphologicalRule>
                </MorphologicalRuleDefinitions>
                <LexicalEntries>
                  <LexicalEntry id="eFen" partOfSpeech="posX">
                    <Allomorphs><Allomorph id="aFen"><PhoneticShape>fen</PhoneticShape></Allomorph></Allomorphs>
                    <MorphemeId>FEN</MorphemeId>
                    <Gloss>fen</Gloss>
                  </LexicalEntry>
                </LexicalEntries>
              </Stratum>
            </Strata>
          </Language>
        </HermitCrabInput>
        """;

    private static string WriteTempGrammar()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "hc-identity-signature-drop-" + Guid.NewGuid().ToString("N") + ".xml"
        );
        File.WriteAllText(path, GrammarXml);
        return path;
    }

    // Mirrors BatchCommand.BuildSignature's format without a dependency on the Tool project.
    private static string Signature(IEnumerable<Word> results)
    {
        List<string> signatures = results
            .Select(w =>
                string.Join("+", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Id))
                + "|"
                + w.Shape.ToRegexString(w.Stratum.CharacterDefinitionTable, true)
            )
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        return signatures.Count == 0 ? "-" : string.Join(";", signatures);
    }

    private static string ParseWithFreshMorpher(Language language, string word)
    {
        // maxDegreeOfParallelism: 1 matches ConformanceMorpherFactory.Create's default
        // (useMemoization: true) -- the construction every conformance self-check run actually
        // uses.
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
        return Signature(morpher.ParseWord(word, out _, false).ToList());
    }

    [Test]
    public void Fen_BareRootControl_IdentityWrapsRootDirectly_StableAndCorrect()
    {
        // Control: mrIdentity wraps the root directly, so no two rules share an affix boundary.
        string path = WriteTempGrammar();
        try
        {
            Language language = XmlLanguageLoader.Load(path);
            for (int trial = 0; trial < 10; trial++)
                Assert.That(ParseWithFreshMorpher(language, "fen"), Is.EqualTo("FEN+IDENT|fen;FEN|fen"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Feno_NoIdentityRuleApplies_StableAndCorrect()
    {
        // Control: after mrPtoQ alone the value is q, so mrIdentity (requires p) never applies.
        string path = WriteTempGrammar();
        try
        {
            Language language = XmlLanguageLoader.Load(path);
            for (int trial = 0; trial < 10; trial++)
                Assert.That(ParseWithFreshMorpher(language, "feno"), Is.EqualTo("FEN+PTOQ|feno"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Fenos_IdentityWrapsOutermost_IsFlakyPendingASeparateOrderingDefect()
    {
        // Measured: fix reaches the correct signature 41/100 fresh loads; reverted, 0/100. The
        // remaining gap is a separate ordering defect (#506), not BidirList's random skip-list levels.
        string path = WriteTempGrammar();
        try
        {
            const string CorrectSig = "FEN+PTOQ+QTOP+IDENT|fenos;FEN+PTOQ+QTOP|fenos";
            bool sawCorrect = false;
            for (int trial = 0; trial < 100 && !sawCorrect; trial++)
            {
                Language language = XmlLanguageLoader.Load(path);
                sawCorrect = ParseWithFreshMorpher(language, "fenos") == CorrectSig;
            }

            Assert.That(sawCorrect, Is.True, "the correct signature was never reached in 100 tries");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
