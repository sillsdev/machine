using NSubstitute;
using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation;

[TestFixture]
public class TranslationExtensionsTests
{
    [Test]
    public void Align_ParallelTextRow()
    {
        var knownAlignment = new WordAlignmentMatrix(10, 7, new[] { (0, 0), (6, 3), (7, 5), (8, 4) });
        var row = new ParallelTextRow("text1", new[] { "1" }, new[] { "1" })
        {
            SourceSegment = "maria no daba una bofetada a la bruja verde .".Split(),
            TargetSegment = "mary didn't slap the green witch .".Split(),
            AlignedWordPairs = knownAlignment.ToAlignedWordPairs()
        };

        var estimatedAlignment = new WordAlignmentMatrix(
            10,
            7,
            new[] { (1, 1), (2, 1), (4, 2), (5, 1), (6, 3), (7, 4), (8, 5), (9, 6) }
        );
        var aligner = Substitute.For<IWordAligner>();
        aligner.Align(Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>()).Returns(estimatedAlignment);

        WordAlignmentMatrix alignment = aligner.Align(row);
        Assert.That(alignment.ToString(), Is.EqualTo("0-0 1-1 2-1 4-2 6-3 7-5 8-4 9-6"));
    }
}
