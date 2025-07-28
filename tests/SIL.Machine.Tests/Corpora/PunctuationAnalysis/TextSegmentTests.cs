using NUnit.Framework;

namespace SIL.Machine.Corpora.PunctuationAnalysis;

[TestFixture]
public class TextSegmentTests
{
    [Test]
    public void BuilderInitialization()
    {
        var builder = new TextSegment.Builder();
        var textSegment = builder.Build();

        Assert.That(textSegment.Text, Is.EqualTo(""));
        Assert.IsNull(textSegment.PreviousSegment);
        Assert.IsNull(textSegment.NextSegment);
        Assert.IsTrue(textSegment.ImmediatePrecedingMarker is UsfmMarkerType.NoMarker);
        Assert.That(textSegment.MarkersInPrecedingContext, Has.Count.EqualTo(0));
        Assert.That(textSegment.IndexInVerse, Is.EqualTo(0));
        Assert.That(textSegment.NumSegmentsInVerse, Is.EqualTo(0));
        Assert.IsNull(textSegment.UsfmToken);
    }

    [Test]
    public void BuilderSetText()
    {
        var builder = new TextSegment.Builder();
        var text = "Example text";
        builder.SetText(text);

        Assert.That(builder.Build().Text, Is.EqualTo(text));
    }

    [Test]
    public void BuilderSetPreviousSegment()
    {
        var builder = new TextSegment.Builder();
        var previousSegment = new TextSegment.Builder().SetText("previous segment text").Build();
        builder.SetPreviousSegment(previousSegment);
        var textSegment = builder.Build();

        Assert.That(textSegment.PreviousSegment, Is.EqualTo(previousSegment));
        Assert.IsNull(textSegment.NextSegment);
        Assert.IsTrue(textSegment.ImmediatePrecedingMarker is UsfmMarkerType.NoMarker);
        Assert.That(textSegment.MarkersInPrecedingContext, Has.Count.EqualTo(0));
        Assert.That(textSegment.IndexInVerse, Is.EqualTo(0));
        Assert.That(textSegment.NumSegmentsInVerse, Is.EqualTo(0));
    }

    [Test]
    public void BuilderAddPrecedingMarker()
    {
        var builder = new TextSegment.Builder();
        builder.AddPrecedingMarker(UsfmMarkerType.Chapter);
        var textSegment = builder.Build();

        Assert.IsTrue(textSegment.ImmediatePrecedingMarker is UsfmMarkerType.Chapter);
        Assert.That(textSegment.MarkersInPrecedingContext.SequenceEqual([UsfmMarkerType.Chapter]));
        Assert.IsNull(textSegment.PreviousSegment);
        Assert.IsNull(textSegment.NextSegment);

        builder.AddPrecedingMarker(UsfmMarkerType.Verse);
        textSegment = builder.Build();

        Assert.That(textSegment.ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.Verse));
        Assert.That(
            textSegment.MarkersInPrecedingContext.SequenceEqual([UsfmMarkerType.Chapter, UsfmMarkerType.Verse,])
        );
        Assert.IsNull(textSegment.PreviousSegment);
        Assert.IsNull(textSegment.NextSegment);
    }

    [Test]
    public void BuilderSetUsfmToken()
    {
        var builder = new TextSegment.Builder();
        builder.SetUsfmToken(new UsfmToken("USFM token text"));
        var textSegment = builder.Build();

        Assert.IsNotNull(textSegment.UsfmToken);
        Assert.That(textSegment.UsfmToken.Type, Is.EqualTo(UsfmTokenType.Text));
        Assert.That(textSegment.UsfmToken.Text, Is.EqualTo("USFM token text"));
        Assert.That(textSegment.Text, Is.EqualTo(""));
        Assert.IsNull(textSegment.PreviousSegment);
        Assert.IsNull(textSegment.NextSegment);
    }

    [Test]
    public void Equals()
    {
        var basicSegment = new TextSegment.Builder().SetText("text1").Build();
        var sameTextSegment = new TextSegment.Builder().SetText("text1").Build();
        var differentTextSegment = new TextSegment.Builder().SetText("different text").Build();

        // Assert.That(basicSegment, Is.EqualTo(basicSegment)); //TODO fix
        // Assert.That(basicSegment , Is.Not.EqualTo(new UsfmToken("text1"))); //TODO also here
        Assert.That(basicSegment, Is.EqualTo(sameTextSegment));
        Assert.That(basicSegment, Is.Not.EqualTo(differentTextSegment));

        var segmentWithIndex = new TextSegment.Builder().SetText("text1").Build();
        segmentWithIndex.IndexInVerse = 1;
        var segmentWithSameIndex = new TextSegment.Builder().SetText("text1").Build();
        segmentWithSameIndex.IndexInVerse = 1;
        var segmentWithDifferentIndex = new TextSegment.Builder().SetText("text1").Build();
        segmentWithDifferentIndex.IndexInVerse = 2;

        Assert.That(segmentWithIndex, Is.EqualTo(segmentWithSameIndex));
        Assert.IsTrue(segmentWithIndex != segmentWithDifferentIndex);
        Assert.IsTrue(segmentWithIndex != basicSegment);

        var segmentWithPrecedingMarker = (
            new TextSegment.Builder().SetText("text1").AddPrecedingMarker(UsfmMarkerType.Verse).Build()
        );
        var segmentWithSamePrecedingMarker = (
            new TextSegment.Builder().SetText("text1").AddPrecedingMarker(UsfmMarkerType.Verse).Build()
        );
        var segmentWithDifferentPrecedingMarker = (
            new TextSegment.Builder().SetText("text1").AddPrecedingMarker(UsfmMarkerType.Chapter).Build()
        );
        var segmentWithMultiplePrecedingMarkers = (
            new TextSegment.Builder()
                .SetText("text1")
                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                .AddPrecedingMarker(UsfmMarkerType.Verse)
                .Build()
        );

        var usfmToken = new UsfmToken("USFM token text");
        var segmentWithUsfmToken = new TextSegment.Builder().SetText("text1").SetUsfmToken(usfmToken).Build();
        var segmentWithSameUsfmToken = new TextSegment.Builder().SetText("text1").SetUsfmToken(usfmToken).Build();
        var segmentWithDifferentUsfmToken = (
            new TextSegment.Builder().SetText("text1").SetUsfmToken(new UsfmToken("Different USFM token text")).Build()
        );

        Assert.That(segmentWithUsfmToken, Is.EqualTo(segmentWithSameUsfmToken));
        Assert.IsTrue(segmentWithUsfmToken != segmentWithDifferentUsfmToken);
        Assert.IsTrue(basicSegment != segmentWithUsfmToken);

        // attributes that are not used in equality checks
        var segmentWithNumVerses = new TextSegment.Builder().SetText("text1").Build();
        segmentWithNumVerses.NumSegmentsInVerse = 3;
        var segmentWithSameNumVerses = new TextSegment.Builder().SetText("text1").Build();
        segmentWithSameNumVerses.NumSegmentsInVerse = 3;
        var segmentWithDifferentNumVerses = new TextSegment.Builder().SetText("text1").Build();
        segmentWithDifferentNumVerses.NumSegmentsInVerse = 4;

        Assert.That(segmentWithNumVerses, Is.EqualTo(segmentWithSameNumVerses));
        Assert.IsTrue(segmentWithNumVerses != segmentWithDifferentNumVerses);
        Assert.IsTrue(segmentWithNumVerses != basicSegment);

        Assert.That(segmentWithPrecedingMarker, Is.EqualTo(segmentWithSamePrecedingMarker));
        Assert.IsTrue(segmentWithPrecedingMarker != segmentWithDifferentPrecedingMarker);
        Assert.That(segmentWithPrecedingMarker, Is.EqualTo(segmentWithMultiplePrecedingMarkers));
        Assert.IsTrue(segmentWithPrecedingMarker != basicSegment);

        var segmentWithPreviousSegment = new TextSegment.Builder().SetText("text1").Build();
        segmentWithPreviousSegment.PreviousSegment = segmentWithNumVerses;

        var segmentWithNextSegment = new TextSegment.Builder().SetText("text1").Build();
        segmentWithNextSegment.NextSegment = segmentWithNumVerses;

        Assert.That(basicSegment, Is.EqualTo(segmentWithPreviousSegment));
        Assert.That(basicSegment, Is.EqualTo(segmentWithNextSegment));
    }

    [Test]
    public void GetText()
    {
        var textSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.That(textSegment.Text, Is.EqualTo("example text"));

        textSegment = new TextSegment.Builder().SetText("new example text").Build();
        Assert.That(textSegment.Text, Is.EqualTo("new example text"));
    }

    [Test]
    public void Length()
    {
        var textSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.That(textSegment.Length, Is.EqualTo("example text".Length));

        textSegment = new TextSegment.Builder().SetText("new example text").Build();
        Assert.That(textSegment.Length, Is.EqualTo("new example text".Length));
    }

    [Test]
    public void SubstringBefore()
    {
        var textSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.That(textSegment.SubstringBefore(7), Is.EqualTo("example"));
        Assert.That(textSegment.SubstringBefore(8), Is.EqualTo("example "));
        Assert.That(textSegment.SubstringBefore(0), Is.EqualTo(""));
        Assert.That(textSegment.SubstringBefore(12), Is.EqualTo("example text"));
    }

    [Test]
    public void SubstringAfter()
    {
        var textSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.That(textSegment.SubstringAfter(7), Is.EqualTo(" text"));
        Assert.That(textSegment.SubstringAfter(8), Is.EqualTo("text"));
        Assert.That(textSegment.SubstringAfter(0), Is.EqualTo("example text"));
        Assert.That(textSegment.SubstringAfter(12), Is.EqualTo(""));
        Assert.That(textSegment.SubstringAfter(11), Is.EqualTo("t"));
    }

    [Test]
    public void IsMarkerInPrecedingContext()
    {
        var noPrecedingMarkerSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.IsFalse(noPrecedingMarkerSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Chapter));
        Assert.IsFalse(noPrecedingMarkerSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse));
        Assert.IsFalse(noPrecedingMarkerSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Character));

        var onePrecedingMarkerTextSegment = (
            new TextSegment.Builder().SetText("example text").AddPrecedingMarker(UsfmMarkerType.Character).Build()
        );

        Assert.IsTrue(onePrecedingMarkerTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Character));
        Assert.IsFalse(onePrecedingMarkerTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse));
        Assert.IsFalse(onePrecedingMarkerTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Chapter));

        var twoPrecedingMarkersTextSegment = (
            new TextSegment.Builder()
                .SetText("example text")
                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                .AddPrecedingMarker(UsfmMarkerType.Verse)
                .Build()
        );
        Assert.IsTrue(twoPrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Chapter));
        Assert.IsTrue(twoPrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse));
        Assert.IsFalse(twoPrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Character));

        var threePrecedingMarkersTextSegment = (
            new TextSegment.Builder()
                .SetText("example text")
                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                .AddPrecedingMarker(UsfmMarkerType.Verse)
                .AddPrecedingMarker(UsfmMarkerType.Character)
                .Build()
        );
        Assert.IsTrue(threePrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Chapter));
        Assert.IsTrue(threePrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse));
        Assert.IsTrue(threePrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Character));
    }

    [Test]
    public void IsFirstSegmentInVerse()
    {
        var textSegment = new TextSegment.Builder().SetText("example text").Build();
        textSegment.IndexInVerse = 0;
        Assert.IsTrue(textSegment.IsFirstSegmentInVerse());

        textSegment.IndexInVerse = 1;
        Assert.IsFalse(textSegment.IsFirstSegmentInVerse());
    }

    [Test]
    public void IsLastSegmentInVerse()
    {
        var textSegment = new TextSegment.Builder().SetText("example text").Build();
        textSegment.IndexInVerse = 0;
        textSegment.NumSegmentsInVerse = 1;
        Assert.IsTrue(textSegment.IsLastSegmentInVerse());

        textSegment.IndexInVerse = 0;
        textSegment.NumSegmentsInVerse = 2;
        Assert.IsFalse(textSegment.IsLastSegmentInVerse());

        textSegment.IndexInVerse = 1;
        Assert.IsTrue(textSegment.IsLastSegmentInVerse());
    }

    [Test]
    public void ReplaceSubstring()
    {
        var textSegment = new TextSegment.Builder().SetText("example text").Build();
        textSegment.ReplaceSubstring(0, 7, "sample");
        Assert.That(textSegment.Text, Is.EqualTo("sample text"));

        textSegment.ReplaceSubstring(7, 11, "text");
        Assert.That(textSegment.Text, Is.EqualTo("sample text"));

        textSegment.ReplaceSubstring(0, 7, "");
        Assert.That(textSegment.Text, Is.EqualTo("text"));

        textSegment.ReplaceSubstring(0, 4, "new'");
        Assert.That(textSegment.Text, Is.EqualTo("new'"));

        textSegment.ReplaceSubstring(3, 4, "\u2019");
        Assert.That(textSegment.Text, Is.EqualTo("new\u2019"));

        textSegment.ReplaceSubstring(0, 0, "prefix ");
        Assert.That(textSegment.Text, Is.EqualTo("prefix new\u2019"));

        textSegment.ReplaceSubstring(0, 0, "");
        Assert.That(textSegment.Text, Is.EqualTo("prefix new\u2019"));

        textSegment.ReplaceSubstring(11, 11, " suffix");
        Assert.That(textSegment.Text, Is.EqualTo("prefix new\u2019 suffix"));

        textSegment.ReplaceSubstring(6, 6, "-");
        Assert.That(textSegment.Text, Is.EqualTo("prefix- new\u2019 suffix"));
    }
}
