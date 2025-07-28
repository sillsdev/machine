using NUnit.Framework;

namespace SIL.Machine.Corpora.PunctuationAnalysis;

[TestFixture]
public class VerseTests
{
    [Test]
    public void InitializeVerse()
    {
        List<TextSegment> textSegments =
        [
            new TextSegment.Builder().SetText("Segment 1").Build(),
            new TextSegment.Builder().SetText("Segment 2").Build(),
            new TextSegment.Builder().SetText("Segment 3").Build(),
        ];

        var verse = new Verse(textSegments);

        Assert.That(verse.TextSegments, Has.Count.EqualTo(3));
        Assert.That(verse.TextSegments, Is.EqualTo(textSegments));
    }

    [Test]
    public void SegmentIndices()
    {
        List<TextSegment> textSegments =
        [
            new TextSegment.Builder().SetText("Segment 1").Build(),
            new TextSegment.Builder().SetText("Segment 1").Build(),
            new TextSegment.Builder().SetText("Segment 1").Build(),
        ];

        var verse = new Verse(textSegments);

        Assert.That(verse.TextSegments[0].IndexInVerse, Is.EqualTo(0));
        Assert.That(verse.TextSegments[1].IndexInVerse, Is.EqualTo(1));
        Assert.That(verse.TextSegments[2].IndexInVerse, Is.EqualTo(2));
    }

    [Test]
    public void NumSegmentsInVerse()
    {
        List<TextSegment> textSegments =
        [
            new TextSegment.Builder().SetText("Segment 1").Build(),
            new TextSegment.Builder().SetText("Segment 2").Build(),
            new TextSegment.Builder().SetText("Segment 3").Build(),
        ];

        var verse = new Verse(textSegments);

        Assert.That(verse.TextSegments[0].NumSegmentsInVerse, Is.EqualTo(3));
        Assert.That(verse.TextSegments[1].NumSegmentsInVerse, Is.EqualTo(3));
        Assert.That(verse.TextSegments[2].NumSegmentsInVerse, Is.EqualTo(3));
    }
}
