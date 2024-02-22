using NUnit.Framework;

namespace SIL.Machine.Translation;

[TestFixture]
public class FuzzyEditDistanceWordAlignmentMethodTests
{
    [Test]
    public void Align_LastSrcFirstTrg()
    {
        var method = new FuzzyEditDistanceWordAlignmentMethod
        {
            ScoreSelector = (srcSegment, srcIndex, trgSegment, trgIndex) =>
            {
                if (srcIndex == -1 || trgIndex == -1)
                    return 0.1;
                return srcSegment[srcIndex] == trgSegment[trgIndex] ? 0.9 : 0.1;
            }
        };

        WordAlignmentMatrix matrix = method.Align("A B".Split(), "B C".Split());
        Assert.That(matrix.ToString(), Is.EqualTo("1-0"));
    }
}
