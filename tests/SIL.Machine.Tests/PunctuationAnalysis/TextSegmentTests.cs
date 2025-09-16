using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class TextSegmentTests
{
    [Test]
    public void BuilderInitialization()
    {
        var builder = new TextSegment.Builder();
        TextSegment textSegment = builder.Build();

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
        string text = "Example text";
        builder.SetText(text);

        Assert.That(builder.Build().Text, Is.EqualTo(text));
    }

    [Test]
    public void BuilderSetPreviousSegment()
    {
        var builder = new TextSegment.Builder();
        TextSegment previousSegment = new TextSegment.Builder().SetText("previous segment text").Build();
        builder.SetPreviousSegment(previousSegment);
        TextSegment textSegment = builder.Build();

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
        TextSegment textSegment = builder.Build();

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
        TextSegment textSegment = builder.Build();

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
        TextSegment basicSegment = new TextSegment.Builder().SetText("text1").Build();
        TextSegment sameTextSegment = new TextSegment.Builder().SetText("text1").Build();
        TextSegment differentTextSegment = new TextSegment.Builder().SetText("different text").Build();
#pragma warning disable NUnit2009 // The same value has been provided as both the actual and the expected argument
        Assert.That(basicSegment, Is.EqualTo(basicSegment));
#pragma warning restore NUnit2009 // The same value has been provided as both the actual and the expected argument
#pragma warning disable NUnit2021 // Incompatible types for EqualTo constraint
        Assert.That(basicSegment, Is.Not.EqualTo(new UsfmToken("text1")));
#pragma warning restore NUnit2021 // Incompatible types for EqualTo constraint
        Assert.That(basicSegment, Is.EqualTo(sameTextSegment));
        Assert.That(basicSegment, Is.Not.EqualTo(differentTextSegment));

        TextSegment segmentWithIndex = new TextSegment.Builder().SetText("text1").Build();
        segmentWithIndex.IndexInVerse = 1;
        TextSegment segmentWithSameIndex = new TextSegment.Builder().SetText("text1").Build();
        segmentWithSameIndex.IndexInVerse = 1;
        TextSegment segmentWithDifferentIndex = new TextSegment.Builder().SetText("text1").Build();
        segmentWithDifferentIndex.IndexInVerse = 2;

        Assert.That(segmentWithIndex, Is.EqualTo(segmentWithSameIndex));
        Assert.That(segmentWithIndex, Is.Not.EqualTo(segmentWithDifferentIndex));
        Assert.That(segmentWithIndex, Is.Not.EqualTo(basicSegment));

        TextSegment segmentWithPrecedingMarker = (
            new TextSegment.Builder().SetText("text1").AddPrecedingMarker(UsfmMarkerType.Verse).Build()
        );
        TextSegment segmentWithSamePrecedingMarker = (
            new TextSegment.Builder().SetText("text1").AddPrecedingMarker(UsfmMarkerType.Verse).Build()
        );
        TextSegment segmentWithDifferentPrecedingMarker = (
            new TextSegment.Builder().SetText("text1").AddPrecedingMarker(UsfmMarkerType.Chapter).Build()
        );
        TextSegment segmentWithMultiplePrecedingMarkers = (
            new TextSegment.Builder()
                .SetText("text1")
                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                .AddPrecedingMarker(UsfmMarkerType.Verse)
                .Build()
        );

        var usfmToken = new UsfmToken("USFM token text");
        TextSegment segmentWithUsfmToken = new TextSegment.Builder().SetText("text1").SetUsfmToken(usfmToken).Build();
        TextSegment segmentWithSameUsfmToken = new TextSegment.Builder()
            .SetText("text1")
            .SetUsfmToken(usfmToken)
            .Build();
        TextSegment segmentWithDifferentUsfmToken = (
            new TextSegment.Builder().SetText("text1").SetUsfmToken(new UsfmToken("Different USFM token text")).Build()
        );

        Assert.That(segmentWithUsfmToken, Is.EqualTo(segmentWithSameUsfmToken));
        Assert.IsTrue(segmentWithUsfmToken != segmentWithDifferentUsfmToken);
        Assert.IsTrue(basicSegment != segmentWithUsfmToken);

        // attributes that are not used in equality checks
        TextSegment segmentWithNumVerses = new TextSegment.Builder().SetText("text1").Build();
        segmentWithNumVerses.NumSegmentsInVerse = 3;
        TextSegment segmentWithSameNumVerses = new TextSegment.Builder().SetText("text1").Build();
        segmentWithSameNumVerses.NumSegmentsInVerse = 3;
        TextSegment segmentWithDifferentNumVerses = new TextSegment.Builder().SetText("text1").Build();
        segmentWithDifferentNumVerses.NumSegmentsInVerse = 4;

        Assert.That(segmentWithNumVerses, Is.EqualTo(segmentWithSameNumVerses));
        Assert.That(segmentWithNumVerses, Is.Not.EqualTo(segmentWithDifferentNumVerses));
        Assert.That(segmentWithNumVerses, Is.Not.EqualTo(basicSegment));

        Assert.That(segmentWithPrecedingMarker, Is.EqualTo(segmentWithSamePrecedingMarker));
        Assert.That(segmentWithPrecedingMarker, Is.Not.EqualTo(segmentWithDifferentPrecedingMarker));
        Assert.That(segmentWithPrecedingMarker, Is.EqualTo(segmentWithMultiplePrecedingMarkers));
        Assert.That(segmentWithPrecedingMarker, Is.Not.EqualTo(basicSegment));

        TextSegment segmentWithPreviousSegment = new TextSegment.Builder().SetText("text1").Build();
        segmentWithPreviousSegment.PreviousSegment = segmentWithNumVerses;

        TextSegment segmentWithNextSegment = new TextSegment.Builder().SetText("text1").Build();
        segmentWithNextSegment.NextSegment = segmentWithNumVerses;

        Assert.That(basicSegment, Is.EqualTo(segmentWithPreviousSegment));
        Assert.That(basicSegment, Is.EqualTo(segmentWithNextSegment));
    }

    [Test]
    public void GetText()
    {
        TextSegment textSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.That(textSegment.Text, Is.EqualTo("example text"));

        textSegment = new TextSegment.Builder().SetText("new example text").Build();
        Assert.That(textSegment.Text, Is.EqualTo("new example text"));
    }

    [Test]
    public void Length()
    {
        TextSegment textSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.That(textSegment.Length, Is.EqualTo("example text".Length));

        textSegment = new TextSegment.Builder().SetText("new example text").Build();
        Assert.That(textSegment.Length, Is.EqualTo("new example text".Length));

        //Combining characters
        textSegment = new TextSegment.Builder().SetText("उत्पत्ति पुस्तकले").Build();
        Assert.That(textSegment.Length, Is.EqualTo(17));

        //Surrogate pairs
        textSegment = new TextSegment.Builder().SetText("𝜺𝜺").Build();
        Assert.That(textSegment.Length, Is.EqualTo(2));
    }

    [Test]
    public void SubstringBefore()
    {
        TextSegment textSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.That(textSegment.SubstringBefore(7), Is.EqualTo("example"));
        Assert.That(textSegment.SubstringBefore(8), Is.EqualTo("example "));
        Assert.That(textSegment.SubstringBefore(0), Is.EqualTo(""));
        Assert.That(textSegment.SubstringBefore(12), Is.EqualTo("example text"));
    }

    [Test]
    public void SubstringAfter()
    {
        TextSegment textSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.That(textSegment.SubstringAfter(7), Is.EqualTo(" text"));
        Assert.That(textSegment.SubstringAfter(8), Is.EqualTo("text"));
        Assert.That(textSegment.SubstringAfter(0), Is.EqualTo("example text"));
        Assert.That(textSegment.SubstringAfter(12), Is.EqualTo(""));
        Assert.That(textSegment.SubstringAfter(11), Is.EqualTo("t"));
    }

    [Test]
    public void IsMarkerInPrecedingContext()
    {
        TextSegment noPrecedingMarkerSegment = new TextSegment.Builder().SetText("example text").Build();
        Assert.IsFalse(noPrecedingMarkerSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Chapter));
        Assert.IsFalse(noPrecedingMarkerSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse));
        Assert.IsFalse(noPrecedingMarkerSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Character));

        TextSegment onePrecedingMarkerTextSegment = (
            new TextSegment.Builder().SetText("example text").AddPrecedingMarker(UsfmMarkerType.Character).Build()
        );

        Assert.IsTrue(onePrecedingMarkerTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Character));
        Assert.IsFalse(onePrecedingMarkerTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse));
        Assert.IsFalse(onePrecedingMarkerTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Chapter));

        TextSegment twoPrecedingMarkersTextSegment = (
            new TextSegment.Builder()
                .SetText("example text")
                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                .AddPrecedingMarker(UsfmMarkerType.Verse)
                .Build()
        );
        Assert.IsTrue(twoPrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Chapter));
        Assert.IsTrue(twoPrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse));
        Assert.IsFalse(twoPrecedingMarkersTextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Character));

        TextSegment threePrecedingMarkersTextSegment = (
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
        TextSegment textSegment = new TextSegment.Builder().SetText("example text").Build();
        textSegment.IndexInVerse = 0;
        Assert.IsTrue(textSegment.IsFirstSegmentInVerse());

        textSegment.IndexInVerse = 1;
        Assert.IsFalse(textSegment.IsFirstSegmentInVerse());
    }

    [Test]
    public void IsLastSegmentInVerse()
    {
        TextSegment textSegment = new TextSegment.Builder().SetText("example text").Build();
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
        TextSegment textSegment = new TextSegment.Builder().SetText("example text").Build();
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
