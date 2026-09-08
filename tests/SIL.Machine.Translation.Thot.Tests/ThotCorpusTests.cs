using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot;

[TestFixture]
public class ThotCorpusTests
{
    [TestCase(ThotWordAlignmentModelType.FastAlign)]
    [TestCase(ThotWordAlignmentModelType.Ibm1)]
    public void WordAlignCorpus_TransductiveMatchesInference(ThotWordAlignmentModelType modelType)
    {
        IParallelTextCorpus corpus = TestHelpers.CreateTestParallelCorpus();
        corpus = corpus.WordAlign(modelType);

        // For deterministic models, the alignments retained during training match those produced by a
        // separate inference pass, so the transductive output must equal aligning each row directly.
        IReadOnlyList<string> transductive = TestHelpers.AlignmentStrings(corpus);

        using ThotSymmetrizedWordAlignmentModel model = TestHelpers.CreateTrainedModel(
            TestHelpers.CreateTestParallelCorpus(),
            modelType
        );
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

    [TestCase(ThotWordAlignmentModelType.Eflomal)]
    [TestCase(ThotWordAlignmentModelType.FastAlign)]
    public void WordAlignCorpus_DefaultIsTransductive(ThotWordAlignmentModelType modelType)
    {
        IParallelTextCorpus corpus = TestHelpers.CreateTestParallelCorpus();
        corpus = corpus.WordAlign(modelType);
        List<ParallelTextRow> rows = [.. corpus.GetRows()];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows, Has.Count.EqualTo(8));
            Assert.That(rows.Any(row => row.AlignedWordPairs.Count > 0), Is.True);
        }
    }
}
