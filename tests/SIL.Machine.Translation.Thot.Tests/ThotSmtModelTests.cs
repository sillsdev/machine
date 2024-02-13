using NUnit.Framework;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot;

[TestFixture]
public class ThotSmtModelTests
{
    [Test]
    public async Task TranslateAsync_TargetSegment_Hmm()
    {
        using ThotSmtModel smtModel = CreateHmmModel();
        TranslationResult result = await smtModel.TranslateAsync("voy a marcharme hoy por la tarde .");
        Assert.That(result.Translation, Is.EqualTo("i am leaving today in the afternoon ."));
    }

    [Test]
    public async Task TranslateAsync_NBestLessThanN_Hmm()
    {
        using ThotSmtModel smtModel = CreateHmmModel();
        IEnumerable<TranslationResult> results = await smtModel.TranslateAsync(3, "voy a marcharme hoy por la tarde .");
        Assert.That(
            results.Select(tr => tr.Translation),
            Is.EqualTo(new[] { "i am leaving today in the afternoon ." })
        );
    }

    [Test]
    public async Task TranslateAsync_NBest_Hmm()
    {
        using ThotSmtModel smtModel = CreateHmmModel();
        IEnumerable<TranslationResult> results = await smtModel.TranslateAsync(2, "hablé hasta cinco en punto .");
        Assert.That(
            results.Select(tr => tr.Translation),
            Is.EqualTo(new[] { "hablé until five o ' clock .", "hablé until five o ' clock for" })
        );
    }

    [Test]
    public async Task TrainSegmentAsync_Segment_Hmm()
    {
        using ThotSmtModel smtModel = CreateHmmModel();
        TranslationResult result = await smtModel.TranslateAsync("esto es una prueba .");
        Assert.That(result.Translation, Is.EqualTo("esto is a prueba ."));
        await smtModel.TrainSegmentAsync("esto es una prueba .", "this is a test .");
        result = await smtModel.TranslateAsync("esto es una prueba .");
        Assert.That(result.Translation, Is.EqualTo("this is a test ."));
    }

    [Test]
    public async Task GetBestPhraseAlignmentAsync_SegmentPair_Hmm()
    {
        using ThotSmtModel smtModel = CreateHmmModel();
        TranslationResult result = await smtModel.GetBestPhraseAlignmentAsync(
            "esto es una prueba .",
            "this is a test ."
        );
        Assert.That(result.Translation, Is.EqualTo("this is a test ."));
    }

    [Test]
    public async Task GetWordGraphAsync_EmptySegment_Hmm()
    {
        using ThotSmtModel smtModel = CreateHmmModel();
        WordGraph wordGraph = await smtModel.GetWordGraphAsync("");
        Assert.That(wordGraph.IsEmpty, Is.True);
    }

    [Test]
    public async Task TranslateBatchAsync_Batch_Hmm()
    {
        string[] batch =
        [
            "por favor , desearía reservar una habitación hasta mañana .",
            "por favor , despiértenos mañana a las siete y cuarto .",
            "voy a marcharme hoy por la tarde .",
            "por favor , ¿ les importaría bajar nuestro equipaje a la habitación número cero trece ?",
            "¿ me podrían dar la llave de la habitación dos cuatro cuatro , por favor ?"
        ];

        using ThotSmtModel smtModel = CreateHmmModel();
        IReadOnlyList<TranslationResult> results = await smtModel.TranslateBatchAsync(batch);
        Assert.That(
            results.Select(tr => tr.Translation),
            Is.EqualTo(
                new[]
                {
                    "please i would like to book a room until tomorrow .",
                    "please wake us up tomorrow at a quarter past seven .",
                    "i am leaving today in the afternoon .",
                    "please would you mind sending down our luggage to room number oh thirteenth ?",
                    "could you give me the key to room number two four four , please ?"
                }
            )
        );
    }

    [Test]
    public void TranslateBatch_Batch_Hmm()
    {
        string[] batch =
        [
            "por favor , desearía reservar una habitación hasta mañana .",
            "por favor , despiértenos mañana a las siete y cuarto .",
            "voy a marcharme hoy por la tarde .",
            "por favor , ¿ les importaría bajar nuestro equipaje a la habitación número cero trece ?",
            "¿ me podrían dar la llave de la habitación dos cuatro cuatro , por favor ?"
        ];

        using ThotSmtModel smtModel = CreateHmmModel();
        IReadOnlyList<TranslationResult> results = smtModel.TranslateBatch(batch);
        Assert.That(
            results.Select(tr => tr.Translation),
            Is.EqualTo(
                new[]
                {
                    "please i would like to book a room until tomorrow .",
                    "please wake us up tomorrow at a quarter past seven .",
                    "i am leaving today in the afternoon .",
                    "please would you mind sending down our luggage to room number oh thirteenth ?",
                    "could you give me the key to room number two four four , please ?"
                }
            )
        );
    }

    [Test]
    public async Task TranslateAsync_TargetSegment_FastAlign()
    {
        using ThotSmtModel smtModel = CreateFastAlignModel();
        TranslationResult result = await smtModel.TranslateAsync("voy a marcharme hoy por la tarde .");
        Assert.That(result.Translation, Is.EqualTo("i am leaving today in the afternoon ."));
    }

    [Test]
    public async Task TranslateAsync_NBestLessThanN_FastAlign()
    {
        using ThotSmtModel smtModel = CreateFastAlignModel();
        IEnumerable<TranslationResult> results = await smtModel.TranslateAsync(3, "voy a marcharme hoy por la tarde .");
        Assert.That(
            results.Select(tr => tr.Translation),
            Is.EqualTo(new[] { "i am leaving today in the afternoon ." })
        );
    }

    [Test]
    public async Task TranslateAsync_NBest_FastAlign()
    {
        using ThotSmtModel smtModel = CreateFastAlignModel();
        IEnumerable<TranslationResult> results = await smtModel.TranslateAsync(2, "hablé hasta cinco en punto .");
        Assert.That(
            results.Select(tr => tr.Translation),
            Is.EqualTo(new[] { "hablé until five o ' clock .", "hablé until five o ' clock , please ." })
        );
    }

    [Test]
    public async Task TrainSegmentAsync_Segment_FastAlign()
    {
        using ThotSmtModel smtModel = CreateFastAlignModel();
        TranslationResult result = await smtModel.TranslateAsync("esto es una prueba .");
        Assert.That(result.Translation, Is.EqualTo("esto is a prueba ."));
        await smtModel.TrainSegmentAsync("esto es una prueba .", "this is a test .");
        result = await smtModel.TranslateAsync("esto es una prueba .");
        Assert.That(result.Translation, Is.EqualTo("this is a test ."));
    }

    [Test]
    public async Task GetBestPhraseAlignmentAsync_SegmentPair_FastAlign()
    {
        using ThotSmtModel smtModel = CreateFastAlignModel();
        TranslationResult result = await smtModel.GetBestPhraseAlignmentAsync(
            "esto es una prueba .",
            "this is a test ."
        );
        Assert.That(result.Translation, Is.EqualTo("this is a test ."));
    }

    [Test]
    public async Task GetWordGraphAsync_EmptySegment_FastAlign()
    {
        using ThotSmtModel smtModel = CreateFastAlignModel();
        WordGraph wordGraph = await smtModel.GetWordGraphAsync("");
        Assert.That(wordGraph.IsEmpty, Is.True);
    }

    [Test]
    public void Constructor_ModelDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(
            () =>
                new ThotSmtModel(
                    ThotWordAlignmentModelType.Hmm,
                    new ThotSmtParameters
                    {
                        TranslationModelFileNamePrefix = "does-not-exist",
                        LanguageModelFileNamePrefix = "does-not-exist"
                    }
                )
        );
    }

    [Test]
    public void Constructor_ModelCorrupted()
    {
        using var tempDir = new TempDirectory("ThotSmtModelTests");
        string tmDir = Path.Combine(tempDir.Path, "tm");
        Directory.CreateDirectory(tmDir);
        File.WriteAllText(Path.Combine(tmDir, "src_trg.ttable"), "corrupted");
        string lmDir = Path.Combine(tempDir.Path, "lm");
        Directory.CreateDirectory(lmDir);
        File.WriteAllText(Path.Combine(lmDir, "trg.lm"), "corrupted");
        Assert.Throws<InvalidOperationException>(
            () =>
                new ThotSmtModel(
                    ThotWordAlignmentModelType.Hmm,
                    new ThotSmtParameters
                    {
                        TranslationModelFileNamePrefix = Path.Combine(tmDir, "src_trg"),
                        LanguageModelFileNamePrefix = Path.Combine(lmDir, "trg.lm")
                    }
                )
        );
    }

    private static ThotSmtModel CreateHmmModel()
    {
        return new ThotSmtModel(ThotWordAlignmentModelType.Hmm, TestHelpers.ToyCorpusHmmConfigFileName);
    }

    private static ThotSmtModel CreateFastAlignModel()
    {
        return new ThotSmtModel(ThotWordAlignmentModelType.FastAlign, TestHelpers.ToyCorpusFastAlignConfigFileName);
    }
}
