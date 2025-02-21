using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class AlignedWordPairTests
{
    [Test]
    public void TryParse_Success()
    {
        string alignmentString = "1-0:0.111111:0.222222 0-1:0.222222:0.111111 2-NULL:0.111111:0.222222";
        Assert.That(
            AlignedWordPair.TryParse(alignmentString, out IReadOnlyCollection<AlignedWordPair> alignedWordPairs),
            Is.True
        );
        Assert.That(alignedWordPairs, Has.Count.EqualTo(3));
        Assert.That(alignedWordPairs.First().TranslationScore, Is.EqualTo(0.111111));
        Assert.That(alignedWordPairs.First().AlignmentScore, Is.EqualTo(0.222222));
        alignmentString = "1-0:0.111111";
        Assert.That(AlignedWordPair.TryParse(alignmentString, out alignedWordPairs), Is.True);
        Assert.That(alignedWordPairs, Has.Count.EqualTo(1));
        Assert.That(alignedWordPairs.First().TranslationScore, Is.EqualTo(0.111111));
    }

    [Test]
    [TestCase("1-0:0.111111:0.2222220-1:0.222222:0.111111 2-NULL:0.111111:0.222222")]
    [TestCase("1-0:0.111111:0.222222 0-1:0.222222:0.111111 A-B:0.111111:0.222222")]
    [TestCase("1-0:0.111111:0.222222 0-1:0x34:0.111111 A-B:0.111111:0.222222")]
    [TestCase("1-0 0-1-3 2-NULL")]
    public void TryParse_Fail(string alignmentString)
    {
        Assert.That(AlignedWordPair.TryParse(alignmentString, out _), Is.False);
    }

    [Test]
    public void ParseToString()
    {
        IEnumerable<AlignedWordPair> alignedWordPairs = new List<AlignedWordPair>()
        {
            new AlignedWordPair(0, 1) { TranslationScore = 0.1, AlignmentScore = 0.1 },
            new AlignedWordPair(1, 0) { TranslationScore = 0.1, AlignmentScore = 0.1 }
        };
        string alignmentString = string.Join(' ', alignedWordPairs.Select(wp => wp.ToString()));
        IEnumerable<AlignedWordPair> parsedAlignedWordPairs = AlignedWordPair.Parse(alignmentString);
        Assert.That(parsedAlignedWordPairs.SequenceEqual(alignedWordPairs));
        Assert.That(
            parsedAlignedWordPairs.First().TranslationScore,
            Is.EqualTo(alignedWordPairs.First().TranslationScore)
        );
        Assert.That(parsedAlignedWordPairs.First().AlignmentScore, Is.EqualTo(alignedWordPairs.First().AlignmentScore));
    }
}
