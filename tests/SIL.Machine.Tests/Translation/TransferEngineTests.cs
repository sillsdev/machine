using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Morphology;
using SIL.ObjectModel;

namespace SIL.Machine.Translation;

[TestFixture]
public class TransferEngineTests
{
    [Test]
    public async Task TranslateAsync_CanTranslate_ReturnsCorrectTranslation()
    {
        IMorphologicalAnalyzer sourceAnalyzer = Substitute.For<IMorphologicalAnalyzer>();
        sourceAnalyzer.AddAnalyses(
            "habló",
            new WordAnalysis(
                new[]
                {
                    new TestMorpheme("s1", "v", "talk", MorphemeType.Stem),
                    new TestMorpheme("s2", "v", "pst", MorphemeType.Affix)
                },
                0,
                "v"
            )
        );
        IMorphologicalGenerator targetGenerator = Substitute.For<IMorphologicalGenerator>();
        var targetMorphemes = new ReadOnlyObservableList<IMorpheme>(
            new ObservableList<IMorpheme>
            {
                new TestMorpheme("e1", "v", "talk", MorphemeType.Stem),
                new TestMorpheme("e2", "v", "pst", MorphemeType.Affix)
            }
        );
        targetGenerator.Morphemes.Returns(targetMorphemes);
        targetGenerator.AddGeneratedWords(
            new WordAnalysis(new[] { targetMorphemes[0], targetMorphemes[1] }, 0, "v"),
            "talked"
        );
        var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
        var engine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
        TranslationResult result = await engine.TranslateAsync("habló");
        Assert.That(result.Translation, Is.EqualTo("talked"));
    }

    [Test]
    public async Task TranslateAsync_CannotAnalyze_ReturnsEmptyTranslation()
    {
        IMorphologicalAnalyzer sourceAnalyzer = Substitute.For<IMorphologicalAnalyzer>();
        sourceAnalyzer.AddAnalyses("habló");
        IMorphologicalGenerator targetGenerator = Substitute.For<IMorphologicalGenerator>();
        var targetMorphemes = new ReadOnlyObservableList<IMorpheme>(
            new ObservableList<IMorpheme>
            {
                new TestMorpheme("e1", "v", "talk", MorphemeType.Stem),
                new TestMorpheme("e2", "v", "pst", MorphemeType.Affix)
            }
        );
        targetGenerator.Morphemes.Returns(targetMorphemes);
        targetGenerator.AddGeneratedWords(
            new WordAnalysis(new[] { targetMorphemes[0], targetMorphemes[1] }, 0, "v"),
            "talked"
        );
        var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
        var engine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
        TranslationResult result = await engine.TranslateAsync("habló");
        Assert.That(result.Translation, Is.EqualTo("habló"));
    }

    [Test]
    public async Task TranslateAsync_CannotGenerate_ReturnsEmptyTranslation()
    {
        IMorphologicalAnalyzer sourceAnalyzer = Substitute.For<IMorphologicalAnalyzer>();
        sourceAnalyzer.AddAnalyses(
            "habló",
            new WordAnalysis(
                new[]
                {
                    new TestMorpheme("s1", "v", "talk", MorphemeType.Stem),
                    new TestMorpheme("s2", "v", "pst", MorphemeType.Affix)
                },
                0,
                "v"
            )
        );
        IMorphologicalGenerator targetGenerator = Substitute.For<IMorphologicalGenerator>();
        var targetMorphemes = new ReadOnlyObservableList<IMorpheme>(
            new ObservableList<IMorpheme>
            {
                new TestMorpheme("e1", "v", "talk", MorphemeType.Stem),
                new TestMorpheme("e2", "v", "pst", MorphemeType.Affix)
            }
        );
        targetGenerator.Morphemes.Returns(targetMorphemes);
        targetGenerator.AddGeneratedWords(new WordAnalysis(new[] { targetMorphemes[0], targetMorphemes[1] }, 0, "v"));
        var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
        var engine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
        TranslationResult result = await engine.TranslateAsync("habló");
        Assert.That(result.Translation, Is.EqualTo("habló"));
    }

    [Test]
    public async Task TranslateAsync_CannotMapMorphemes_ReturnsEmptyTranslation()
    {
        IMorphologicalAnalyzer sourceAnalyzer = Substitute.For<IMorphologicalAnalyzer>();
        sourceAnalyzer.AddAnalyses(
            "habló",
            new WordAnalysis(
                new[]
                {
                    new TestMorpheme("s1", "v", "talk", MorphemeType.Stem),
                    new TestMorpheme("s2", "v", "pst", MorphemeType.Affix)
                },
                0,
                "v"
            )
        );
        IMorphologicalGenerator targetGenerator = Substitute.For<IMorphologicalGenerator>();
        var targetMorphemes = new ReadOnlyObservableList<IMorpheme>(new ObservableList<IMorpheme>());
        targetGenerator.Morphemes.Returns(targetMorphemes);
        var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
        var engine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
        TranslationResult result = await engine.TranslateAsync("habló");
        Assert.That(result.Translation, Is.EqualTo("habló"));
    }
}
