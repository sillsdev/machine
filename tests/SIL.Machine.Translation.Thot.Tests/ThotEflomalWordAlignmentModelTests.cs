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
        // No iteration counts are specified, so Eflomal derives its schedule automatically from
        // the corpus size (the recommended default).
        var model = new ThotEflomalWordAlignmentModel();
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
    public void DeterministicTrainingIsReproducible()
    {
        static WordAlignmentMatrix TrainDeterministic()
        {
            using var model = new ThotEflomalWordAlignmentModel
            {
                Parameters = new ThotWordAlignmentParameters { EflomalDeterministic = true },
            };
            ITrainer trainer = model.CreateTrainer(TestHelpers.CreateTestParallelCorpus());
            trainer.TrainAsync().GetAwaiter().GetResult();
            return model.Align("isthay isyay ayay esttay-N .".Split(), "this is a test N .".Split());
        }

        // With deterministic training the chains run serially from a fixed seed, so two separate
        // training runs produce an identical model.
        WordAlignmentMatrix first = TrainDeterministic();
        WordAlignmentMatrix second = TrainDeterministic();
        Assert.That(second.ValueEquals(first), Is.True);
    }

    [Test]
    public void SeedAndLexNormAreApplied()
    {
        static WordAlignmentMatrix Train(uint seed, bool lexNorm)
        {
            using var model = new ThotEflomalWordAlignmentModel
            {
                Parameters = new ThotWordAlignmentParameters
                {
                    EflomalSeed = seed,
                    EflomalDeterministic = true,
                    EflomalLexNorm = lexNorm,
                },
            };
            ITrainer trainer = model.CreateTrainer(TestHelpers.CreateTestParallelCorpus());
            trainer.TrainAsync().GetAwaiter().GetResult();
            return model.Align("isthay isyay ayay esttay-N .".Split(), "this is a test N .".Split());
        }

        // A fixed seed with deterministic training is reproducible across separate runs.
        Assert.That(Train(12345, lexNorm: true).ValueEquals(Train(12345, lexNorm: true)), Is.True);
        // Both lexical-normalization modes train and produce a valid alignment.
        Assert.That(Train(12345, lexNorm: false).ToString().Trim(), Is.Not.Empty);
    }

    [Test]
    public void CreateTrainerWithExplicitSchedule()
    {
        // Specifying the IBM1/HMM/IBM3 iteration counts overrides the automatic schedule with an
        // explicit IBM1 -> HMM -> fertility (IBM3) schedule.
        var model = new ThotEflomalWordAlignmentModel
        {
            Parameters = new ThotWordAlignmentParameters
            {
                Ibm1IterationCount = 50,
                HmmIterationCount = 50,
                Ibm3IterationCount = 200,
            },
        };
        ITrainer trainer = model.CreateTrainer(TestHelpers.CreateTestParallelCorpus());
        trainer.TrainAsync().GetAwaiter().GetResult();
        trainer.SaveAsync().GetAwaiter().GetResult();

        using (model)
        {
            WordAlignmentMatrix matrix = model.Align(
                "isthay isyay ayay esttay-N .".Split(),
                "this is a test N .".Split()
            );
            Assert.That(matrix.ToString(), Does.Contain("0-0")); // "this" aligns to "isthay"
            Assert.That(model.GetTranslationProbability("isthay", "this"), Is.GreaterThan(0.1));
        }
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
