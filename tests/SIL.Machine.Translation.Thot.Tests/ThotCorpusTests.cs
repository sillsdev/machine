using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot;

[TestFixture(typeof(ThotEflomalWordAlignmentModel))]
[TestFixture(typeof(ThotFastAlignWordAlignmentModel))]
[TestFixture(typeof(ThotIbm1WordAlignmentModel))]
public class ThotCorpusTests<T>
    where T : ThotWordAlignmentModel, new()
{
    [Test]
    public async Task WordAlignCorpus_TransductiveMatchesInference()
    {
        ParallelTextCorpus corpus = TestHelpers.CreateTestParallelCorpus();
        using ThotSymmetrizedWordAlignmentModel aligner = await TestHelpers.CreateWordAligner<T>(corpus);

        // For deterministic models, the alignments retained during training match those produced by a
        // separate inference pass, so the transductive output must equal aligning each row directly.
        IReadOnlyList<string> transductive = TestHelpers.AlignmentStrings(corpus.WordAlign(aligner));

        using var model = new ThotSymmetrizedWordAlignmentModel(new T(), new T());
        model.Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd;
        ITrainer trainer = model.CreateTrainer(TestHelpers.CreateTestParallelCorpus());
        await trainer.TrainAsync();
        await trainer.SaveAsync();
        IReadOnlyList<string> inference =
        [
            .. TestHelpers
                .CreateTestParallelCorpus()
                .GetRows()
                .SelectMany(row =>
                    model
                        .Align(row.SourceSegment, row.TargetSegment)
                        .ToAlignedWordPairs()
                        .Select(wp => new AlignedWordPair(wp.SourceIndex, wp.TargetIndex).ToString())
                ),
        ];
        Assert.That(transductive, Is.EquivalentTo(inference));
    }

    [Test]
    public async Task WordAlignCorpus_DefaultIsTransductive()
    {
        ParallelTextCorpus corpus = TestHelpers.CreateTestParallelCorpus();
        using ThotSymmetrizedWordAlignmentModel aligner = await TestHelpers.CreateWordAligner<T>(corpus);
        List<ParallelTextRow> rows = [.. corpus.WordAlign(aligner).GetRows()];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows, Has.Count.EqualTo(8));
            Assert.That(rows.Any(row => row.AlignedWordPairs.Count > 0), Is.True);
        }
    }
}
