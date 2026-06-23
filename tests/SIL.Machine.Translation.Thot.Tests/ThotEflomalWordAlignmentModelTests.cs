using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot;

[TestFixture]
public class ThotEflomalWordAlignmentModelTests
{
    [OneTimeSetUp]
    public void RequireEflomalSupport()
    {
        bool supported;
        try
        {
            var probe = new ThotWordAlignmentModelTrainer(
                ThotWordAlignmentModelType.Eflomal,
                TestHelpers.CreateTestParallelCorpus(),
                "probe"
            );
            probe.Dispose();
            supported = true;
        }
        catch (NotSupportedException)
        {
            supported = false;
        }
        Assume.That(supported, Is.True, "Eflomal requires a Thot build that includes EflomalAlignmentModel");
    }

    private static ThotEflomalWordAlignmentModel TrainModel(string? prefFileName = null)
    {
        var model = new ThotEflomalWordAlignmentModel
        {
            Parameters = new ThotWordAlignmentParameters { EflomalIterationCount = 12 },
        };
        if (prefFileName != null)
            model.CreateNew(prefFileName);
        ITrainer trainer = model.CreateTrainer(TestHelpers.CreateTestParallelCorpus());
        trainer.TrainAsync().GetAwaiter().GetResult();
        trainer.SaveAsync().GetAwaiter().GetResult();
        return model;
    }

    [Test]
    public void CreateTrainerAndAlign()
    {
        using ThotEflomalWordAlignmentModel model = TrainModel();

        WordAlignmentMatrix matrix = model.Align("isthay isyay ayay esttay-N .".Split(), "this is a test N .".Split());
        // The Bayesian sampler is deterministic for a fixed seed, but we assert structural
        // plausibility rather than an exact matrix so the test is robust to schedule tuning.
        Assert.That(matrix.RowCount, Is.EqualTo(5));
        Assert.That(matrix.ColumnCount, Is.EqualTo(6));
        Assert.That(matrix.ToString(), Does.Contain("0-0")); // "this" aligns to "isthay"
        Assert.That(matrix.ToString().Trim(), Is.Not.Empty);
    }

    [Test]
    public void AlignBatch()
    {
        using ThotEflomalWordAlignmentModel model = TrainModel();

        (string, string)[] batch =
        {
            ("isthay isyay ayay esttay-N .", "this is a test N ."),
            ("ouyay ouldshay esttay-V oftenyay .", "you should test V often ."),
            ("isyay isthay orkingway ?", "is this working ?"),
        };
        IReadOnlyList<WordAlignmentMatrix> alignments = model.AlignBatch(
            batch
                .Select(p => ((IReadOnlyList<string>)p.Item1.Split(), (IReadOnlyList<string>)p.Item2.Split()))
                .ToArray()
        );
        Assert.That(alignments.Count, Is.EqualTo(3));
        Assert.That(alignments.All(m => !string.IsNullOrWhiteSpace(m.ToString())), Is.True);
    }

    [Test]
    public void GetTranslationProbability()
    {
        using ThotEflomalWordAlignmentModel model = TrainModel();

        // After training the model should strongly associate the obvious translation.
        Assert.That(model.GetTranslationProbability("isthay", "this"), Is.GreaterThan(0.1));
    }

    [Test]
    public void SourceAndTargetWords()
    {
        using ThotEflomalWordAlignmentModel model = TrainModel();

        Assert.That(model.SourceWords.Count, Is.GreaterThan(0));
        Assert.That(model.TargetWords.Count, Is.GreaterThan(0));
    }

    [Test]
    public void SaveAndLoadRoundTrip()
    {
        using var tempDir = new TempDirectory("ThotEflomalWordAlignmentModelTests");
        string prefix = Path.Combine(tempDir.Path, "src_trg_invswm");

        WordAlignmentMatrix trained;
        using (ThotEflomalWordAlignmentModel model = TrainModel(prefix))
        {
            trained = model.Align("isthay isyay ayay esttay-N .".Split(), "this is a test N .".Split());
        }

        using var loaded = new ThotEflomalWordAlignmentModel(prefix);
        WordAlignmentMatrix reloaded = loaded.Align(
            "isthay isyay ayay esttay-N .".Split(),
            "this is a test N .".Split()
        );
        Assert.That(reloaded.ValueEquals(trained), Is.True);
    }

    [Test]
    public void SymmetrizedAlignment()
    {
        using var tempDir = new TempDirectory("ThotEflomalWordAlignmentModelTests");
        using var direct = TrainModel(Path.Combine(tempDir.Path, "src_trg_invswm"));
        using var inverse = new ThotEflomalWordAlignmentModel();
        ITrainer invTrainer = inverse.CreateTrainer(TestHelpers.CreateTestParallelCorpus().Invert());
        invTrainer.TrainAsync().GetAwaiter().GetResult();
        invTrainer.SaveAsync().GetAwaiter().GetResult();

        using var symmetrized = new SymmetrizedWordAlignmentModel(direct, inverse);
        WordAlignmentMatrix matrix = symmetrized.Align(
            "isthay isyay ayay esttay-N .".Split(),
            "this is a test N .".Split()
        );
        Assert.That(matrix.ToString().Trim(), Is.Not.Empty);
    }
}
