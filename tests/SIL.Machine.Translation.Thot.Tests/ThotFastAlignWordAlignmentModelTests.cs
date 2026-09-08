using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot;

[TestFixture]
public class ThotFastAlignWordAlignmentModelTests
{
    private static string DirectModelPath =>
        Path.Combine(TestHelpers.ToyCorpusFastAlignFolderName, "tm", "src_trg_invswm");
    private static string InverseModelPath =>
        Path.Combine(TestHelpers.ToyCorpusFastAlignFolderName, "tm", "src_trg_swm");

    [Test]
    public void Align()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split();
        string[] targetSegment = "could we see another room , please ?".Split();
        WordAlignmentMatrix alignment = model.Align(sourceSegment, targetSegment);
        Assert.That(alignment.ToString(), Is.EqualTo("0-0 4-1 5-2 6-3 7-4 8-6 8-7"));
    }

    [Test]
    public void AlignBatch()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        (string, string)[] batch =
        {
            (
                "por favor , desearía reservar una habitación hasta mañana .",
                "i would like to book a room until tomorrow , please ."
            ),
            (
                "por favor , despiértenos mañana a las siete y cuarto .",
                "please wake us up tomorrow at a quarter past seven ."
            ),
            ("voy a marcharme hoy por la tarde .", "i am leaving today in the afternoon ."),
            (
                "por favor , ¿ les importaría bajar nuestro equipaje a la habitación número cero trece ?",
                "would you mind sending down our luggage to room number oh one three , please ?"
            ),
            (
                "¿ me podrían dar la llave de la habitación dos cuatro cuatro , por favor ?",
                "could you give me the key to room number two four four , please ?"
            ),
        };
        IReadOnlyList<WordAlignmentMatrix> alignments = model.AlignBatch(
            batch
                .Select(p => ((IReadOnlyList<string>)p.Item1.Split(), (IReadOnlyList<string>)p.Item2.Split()))
                .ToArray()
        );
        Assert.That(
            alignments.Select(m => m.ToString()),
            Is.EqualTo(
                new[]
                {
                    "0-0 3-1 3-2 4-3 4-4 5-5 6-6 7-7 8-8 9-11",
                    "1-0 3-1 3-2 3-3 4-4 6-5 7-9 8-6 8-8 9-7 10-10",
                    "0-0 0-1 2-2 3-3 5-5 6-4 6-6 7-7",
                    "3-1 4-0 5-2 5-3 6-4 7-5 8-6 8-7 10-11 10-12 11-8 12-9 13-10 15-14 15-15",
                    "0-0 0-1 1-3 3-2 4-4 5-5 5-6 7-8 8-7 9-9 11-10 11-11 13-12 14-13 15-14",
                }
            )
        );
    }

    [Test]
    public void GetAvgTranslationScore()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split();
        string[] targetSegment = "could we see another room , please ?".Split();
        WordAlignmentMatrix alignment = model.Align(sourceSegment, targetSegment);
        double score = model.GetAvgTranslationScore(sourceSegment, targetSegment, alignment);
        Assert.That(score, Is.EqualTo(0.34).Within(0.01));
    }

    [Test]
    public void GetTranslationProbability()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        Assert.That(model.GetTranslationProbability("esto", "this"), Is.EqualTo(0.0).Within(0.01));
        Assert.That(model.GetTranslationProbability("es", "is"), Is.EqualTo(0.90).Within(0.01));
        Assert.That(model.GetTranslationProbability("una", "a"), Is.EqualTo(0.83).Within(0.01));
        Assert.That(model.GetTranslationProbability("prueba", "test"), Is.EqualTo(0.0).Within(0.01));
    }

    [Test]
    public void SourceWords_Enumerate()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        Assert.That(model.SourceWords.Count, Is.EqualTo(500));
    }

    [Test]
    public void SourceWords_IndexAccessor()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        Assert.That(model.SourceWords[0], Is.EqualTo("NULL"));
        Assert.That(model.SourceWords[499], Is.EqualTo("pagar"));
    }

    [Test]
    public void SourceWords_Count()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        Assert.That(model.SourceWords.Count, Is.EqualTo(500));
    }

    [Test]
    public void TargetWords_Enumerate()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        Assert.That(model.TargetWords.Count, Is.EqualTo(352));
    }

    [Test]
    public void TargetWords_IndexAccessor()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        Assert.That(model.TargetWords[0], Is.EqualTo("NULL"));
        Assert.That(model.TargetWords[351], Is.EqualTo("pay"));
    }

    [Test]
    public void TargetWords_Count()
    {
        using var model = new ThotFastAlignWordAlignmentModel(DirectModelPath);
        Assert.That(model.TargetWords.Count, Is.EqualTo(352));
    }

    [Test]
    public void GetTranslationTable_SymmetrizedNoThreshold()
    {
        using var model = new SymmetrizedWordAlignmentModel(
            new ThotFastAlignWordAlignmentModel(DirectModelPath),
            new ThotFastAlignWordAlignmentModel(InverseModelPath)
        );
        Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable();
        Assert.That(table.Count, Is.EqualTo(500));
        Assert.That(table["es"].Count, Is.EqualTo(21));
    }

    [Test]
    public void GetTranslationTable_SymmetrizedThreshold()
    {
        using var model = new SymmetrizedWordAlignmentModel(
            new ThotFastAlignWordAlignmentModel(DirectModelPath),
            new ThotFastAlignWordAlignmentModel(InverseModelPath)
        );
        Dictionary<string, Dictionary<string, double>> table = model.GetTranslationTable(0.2);
        Assert.That(table.Count, Is.EqualTo(500));
        Assert.That(table["es"].Count, Is.EqualTo(2));
    }

    [Test]
    public void GetAvgTranslationScore_Symmetrized()
    {
        using var model = new SymmetrizedWordAlignmentModel(
            new ThotFastAlignWordAlignmentModel(DirectModelPath),
            new ThotFastAlignWordAlignmentModel(InverseModelPath)
        );
        string[] sourceSegment = "por favor , ¿ podríamos ver otra habitación ?".Split();
        string[] targetSegment = "could we see another room , please ?".Split();
        WordAlignmentMatrix alignment = model.Align(sourceSegment, targetSegment);
        double score = model.GetAvgTranslationScore(sourceSegment, targetSegment, alignment);
        Assert.That(score, Is.EqualTo(0.36).Within(0.01));
    }

    [Test]
    public void Constructor_ModelCorrupted()
    {
        using var tempDir = new TempDirectory("ThotFastAlignWordAlignmentModelTests");
        string modelPrefix = Path.Combine(tempDir.Path, "src_trg_invswm");
        File.WriteAllText(modelPrefix + ".src", "corrupted");
        Assert.Throws<InvalidOperationException>(() => new ThotFastAlignWordAlignmentModel(modelPrefix));
    }

    [Test]
    public async Task EmitTrainingAlignments_SingleDirection()
    {
        ParallelTextCorpus corpus = TestHelpers.CreateTestParallelCorpus();
        ParallelTextRow row = corpus.GetRows().First();
        using var model = new ThotFastAlignWordAlignmentModel();
        model.EmitTrainingAlignments = true;
        ITrainer trainer = model.CreateTrainer(corpus);
        await trainer.TrainAsync();
        await trainer.SaveAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.TrainingAlignmentCount, Is.EqualTo(8));
            // For a deterministic model, the retained training alignment matches the inference alignment,
            // and it survives the trainer being closed.
            Assert.That(
                model.GetTrainingAlignment(0).ValueEquals(model.Align(row.SourceSegment, row.TargetSegment)),
                Is.True
            );
        }
    }

    [Test]
    public async Task EmitTrainingAlignments_Symmetrized()
    {
        ParallelTextCorpus corpus = TestHelpers.CreateTestParallelCorpus();
        ParallelTextRow row = corpus.GetRows().First();
        using var model = new ThotSymmetrizedWordAlignmentModel(
            new ThotFastAlignWordAlignmentModel(),
            new ThotFastAlignWordAlignmentModel()
        );
        model.EmitTrainingAlignments = true;
        ITrainer trainer = model.CreateTrainer(corpus);
        await trainer.TrainAsync();
        await trainer.SaveAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.TrainingAlignmentCount, Is.EqualTo(8));
            // The C++ symmetrized transductive alignment matches the C++ symmetrized inference alignment.
            Assert.That(
                model.GetTrainingAlignment(0).ValueEquals(model.Align(row.SourceSegment, row.TargetSegment)),
                Is.True
            );
        }
    }

    [Test]
    public async Task EmitTrainingAlignments_Disabled()
    {
        ParallelTextCorpus corpus = TestHelpers.CreateTestParallelCorpus();
        using var model = new ThotFastAlignWordAlignmentModel();
        ITrainer trainer = model.CreateTrainer(corpus);
        await trainer.TrainAsync();
        await trainer.SaveAsync();
        // When emission is not enabled, retrieval returns a degenerate result rather than raising.
        WordAlignmentMatrix alignment = model.GetTrainingAlignment(0);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(alignment.ColumnCount, Is.Zero);
            Assert.That(alignment.RowCount, Is.Zero);
        }
    }

    [Test]
    public void WordAlignCorpus_TransductiveTextIdsKeepIndexInSync()
    {
        // Filtering by text must not desync the training-alignment index: the rows for a requested text
        // must get exactly the alignments they got in the unfiltered pass, not those of earlier rows.
        // The model is trained up front so that both passes read the same training alignments.
        IParallelTextCorpus parallelCorpus = TestHelpers.CreateTwoTextParallelCorpus();
        using ThotSymmetrizedWordAlignmentModel model = TestHelpers.CreateTrainedModel(parallelCorpus);
        IParallelTextCorpus corpus = parallelCorpus.WordAlign(model);
        List<ParallelTextRow> full = [.. corpus.GetRows()];
        IReadOnlyList<string> text2Expected =
        [
            .. full.Skip(2)
                .SelectMany(row =>
                    row.AlignedWordPairs.Select(wp => new AlignedWordPair(wp.SourceIndex, wp.TargetIndex).ToString())
                ),
        ];

        IReadOnlyList<string> text2Actual = TestHelpers.AlignmentStrings(corpus, ["text2"]);
        Assert.That(text2Actual, Is.EqualTo(text2Expected));
    }
}
