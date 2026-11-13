using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

public class SelfOpaquingTests
{
    [TestCase("segment.xml", "da", "ata", "ta")]
    [TestCase("alpha.xml", "mp", "N pa", "mp")]
    [TestCase("glottal.xml", "-a", "POSTC", "ʔa")]
#pragma warning disable CS0618
    [Timeout(15000)]
#pragma warning restore CS0618
    public void IncompatibleEnvironmentTerminatesWithExpectedAnalysis(
        string fixture,
        string surface,
        string expected,
        string synthesized
    )
    {
        string path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "SelfOpaquing", fixture);
        Language language = XmlLanguageLoader.Load(path);
        var morpher = new Morpher(new TraceManager(), language);
        Word[] words = morpher.ParseWord(surface).ToArray();
        string[] analyses = words
            .Select(word => string.Join(" ", word.AllomorphsInMorphOrder.Select(morph => morph.Morpheme.Gloss)))
            .ToArray();
        Assert.That(analyses, Is.EquivalentTo(new[] { expected }));
        Word parsed = words.Single();
        var root = (LexEntry)parsed.RootAllomorph.Morpheme;
        Morpheme[] affixes = parsed
            .AllomorphsInMorphOrder.Select(morph => morph.Morpheme)
            .Where(m => m != root)
            .ToArray();
        Assert.That(morpher.GenerateWords(root, affixes, new FeatureStruct()), Is.EquivalentTo(new[] { synthesized }));
    }
}
