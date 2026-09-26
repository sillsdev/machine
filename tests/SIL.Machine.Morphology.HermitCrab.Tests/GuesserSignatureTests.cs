using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

file static class MiniSignatureFormat
{
    public static string BuildSignature(IEnumerable<Word> results)
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
}

[TestFixture]
public class GuesserSignatureTests
{
    private const string GrammarXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
        <HermitCrabInput>
          <Language>
            <Name>GuesserSignatureProbe</Name>
            <PartsOfSpeech>
              <PartOfSpeech id="posN"><Name>n</Name></PartOfSpeech>
            </PartsOfSpeech>
            <CharacterDefinitionTable id="table1">
              <Name>Main</Name>
              <SegmentDefinitions>
                <SegmentDefinition id="cF"><Representations><Representation>f</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cO"><Representations><Representation>o</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cS"><Representations><Representation>s</Representation></Representations></SegmentDefinition>
              </SegmentDefinitions>
            </CharacterDefinitionTable>
            <NaturalClasses>
              <FeatureNaturalClass id="ncAny"><Name>Any</Name></FeatureNaturalClass>
            </NaturalClasses>
            <Strata>
              <Stratum characterDefinitionTable="table1" morphologicalRuleOrder="unordered" morphologicalRules="mrPlural">
                <Name>Main</Name>
                <MorphologicalRuleDefinitions>
                  <MorphologicalRule id="mrPlural" requiredPartsOfSpeech="posN" outputPartOfSpeech="posN">
                    <Name>plural</Name>
                    <MorphologicalSubrules>
                      <MorphologicalSubrule id="subPlural">
                        <MorphologicalInput><PhoneticSequence id="stemPl"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence></MorphologicalInput>
                        <MorphologicalOutput><CopyFromInput index="stemPl" /><InsertSegments><PhoneticShape>s</PhoneticShape></InsertSegments></MorphologicalOutput>
                      </MorphologicalSubrule>
                    </MorphologicalSubrules>
                    <MorphemeId>PL</MorphemeId>
                  </MorphologicalRule>
                </MorphologicalRuleDefinitions>
                <LexicalEntries>
                  <!-- The Guesser/LexicalGuess pattern root: "[Any]*" is a Kleene-star natural-class
                       pattern (CharacterDefinitionTable.GetShapeNodes' pattern-language branch), which
                       makes RootAllomorph.IsPattern true, so Morpher's constructor routes it into
                       _lexicalPatterns instead of the ordinary trie, exactly what LexicalGuess scans.
                       No MorphemeId/Gloss is given: the pattern entry's own identity is irrelevant, only
                       the dynamically-guessed LexEntry that LexicalGuess constructs from a MATCH against
                       this pattern ever appears in a signature. -->
                  <LexicalEntry id="eGuessPattern" partOfSpeech="posN">
                    <Allomorphs><Allomorph id="aGuessPattern"><PhoneticShape>[Any]*</PhoneticShape></Allomorph></Allomorphs>
                  </LexicalEntry>
                </LexicalEntries>
              </Stratum>
            </Strata>
          </Language>
        </HermitCrabInput>
        """;

    private static string WriteTempGrammar()
    {
        string path = Path.Combine(Path.GetTempPath(), "hc-guesser-signature-" + Guid.NewGuid().ToString("N") + ".xml");
        File.WriteAllText(path, GrammarXml);
        return path;
    }

    // Named for the overload it calls, not the production path: no Tool-project reference means
    // it cannot call ParseOneWord. Proves the fact that path relies on: the 2-argument overload
    // never guesses.
    [Test]
    public void TwoArgumentParseWord_NeverGuesses_ZeroResultsForAnUnknownRoot()
    {
        string path = WriteTempGrammar();
        try
        {
            Language language = XmlLanguageLoader.Load(path);
            var morpher = new Morpher(new TraceManager(), language);

            Word[] results = morpher.ParseWord("foos", out _).ToArray();

            Assert.That(results, Is.Empty);
            Assert.That(MiniSignatureFormat.BuildSignature(results), Is.EqualTo("-"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void GuessRootTrue_RendersGuessedStemAsItsOwnLiteralSurfaceText()
    {
        string path = WriteTempGrammar();
        try
        {
            Language language = XmlLanguageLoader.Load(path);
            var morpher = new Morpher(new TraceManager(), language);

            Word[] results = morpher.ParseWord("foos", out _, guessRoot: true).ToArray();

            Assert.That(results, Is.Not.Empty);
            string signature = MiniSignatureFormat.BuildSignature(results);

            Assert.That(signature, Is.EqualTo("foo+PL|foos;foos|foos"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
