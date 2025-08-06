using NUnit.Framework;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class ChapterTests
{
    [Test]
    public void InitializeVerse()
    {
        List<TextSegment> textSegments1 =
        [
            new TextSegment.Builder().SetText("Segment 1").Build(),
            new TextSegment.Builder().SetText("Segment 2").Build(),
            new TextSegment.Builder().SetText("Segment 3").Build(),
        ];
        var verse1 = new Verse(textSegments1);

        List<TextSegment> textSegments2 =
        [
            new TextSegment.Builder().SetText("Segment 4").Build(),
            new TextSegment.Builder().SetText("Segment 5").Build(),
            new TextSegment.Builder().SetText("Segment 6").Build(),
        ];
        var verse2 = new Verse(textSegments2);

        var chapter = new Chapter([verse1, verse2]);

        Assert.That(chapter.Verses, Has.Count.EqualTo(2));
        Assert.That(chapter.Verses[0].TextSegments, Is.EqualTo(textSegments1));
        Assert.That(chapter.Verses[1].TextSegments, Is.EqualTo(textSegments2));
    }
}
