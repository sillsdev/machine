using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

// This test project does not (and should not, to avoid an unnecessary dependency) reference
// SIL.Machine.Morphology.HermitCrab.Tool, so it can't call the real SignatureFormat.BuildSignature
// directly -- this is a byte-for-byte transcription of that method (see
// src/SIL.Machine.Morphology.HermitCrab.Tool/SignatureFormat.cs), kept here ONLY so this test can
// assert on the exact string a real batch/self-check run would produce.
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

/// <summary>
/// Pins the exact rendering of a <c>LexicalGuess</c> (Guesser) parse in the 5-column batch
/// signature format (<c>SignatureFormat.BuildSignature</c>, transcribed locally below as
/// <see cref="MiniSignatureFormat.BuildSignature"/>).
///
/// Two findings this test documents (see conformance/PROTOCOL.md's guess-stem addendum for the
/// full writeup):
/// <list type="number">
/// <item>The signature's morph-chain component for a guessed root is literally the guessed
/// surface substring itself, e.g. "foo" -- <c>Morpher.LexicalGuess</c> (private) constructs a
/// throwaway <see cref="LexEntry"/> with <c>Id = shapeString</c> (the matched text), and the
/// signature algorithm walks <c>AllomorphsInMorphOrder</c> reading each allomorph's
/// <c>Morpheme.Id</c> -- so a guessed root has no distinguishing marker at all in the signature
/// string; it is indistinguishable, by string alone, from a REAL lexical entry that happened to
/// have the identical id.</item>
/// <item><c>BatchCommand</c> (the CLI `batch` subcommand every conformance adapter/oracle
/// invocation goes through) can NEVER produce a guess-stem parse: <c>SignatureFormat.ParseOneWord</c>
/// calls the 2-argument <c>Morpher.ParseWord(word, out trace)</c> overload, which hardcodes
/// <c>guessRoot: false</c>. No CLI command in SIL.Machine.Morphology.HermitCrab.Tool exposes the
/// 3-argument, <c>guessRoot: true</c> overload at all. This test therefore calls
/// <see cref="Morpher.ParseWord(string, out object, bool)"/> directly -- the only way to exercise
/// this path in this codebase today.</item>
/// </list>
/// </summary>
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

    [Test]
    public void BatchCommandPath_NeverGuesses_ZeroResultsForAnUnknownRoot()
    {
        // "foos" has no real lexical entry for "foo" -- only the guess pattern exists. The
        // 2-argument overload (what SignatureFormat.ParseOneWord, and therefore every `batch`
        // invocation, actually calls) must find nothing.
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
            // Pinned rendering: TWO guesses, because the guess pattern also matches the WHOLE
            // surface string as a bare, unaffixed root (the pattern "[Any]*" is tried against every
            // analysis-direction candidate, including the one where mrPlural was never unapplied at
            // all) -- guessing is not restricted to "the largest/most plausible" segmentation:
            //   - "foo+PL|foos": PL ("-s") unapplied first, "foo" guessed as the root.
            //   - "foos|foos": no rule unapplied at all, the entire string "foos" guessed as the root.
            // Both confirm the core finding either way: a guessed root's morph-chain component is
            // the literal guessed surface substring itself ("foo", or "foos"), exactly as if a real
            // LexEntry with that Id had matched -- a guess parse carries no distinguishing marker in
            // the signature string itself.
            Assert.That(signature, Is.EqualTo("foo+PL|foos;foos|foos"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
