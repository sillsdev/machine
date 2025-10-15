using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class QuoteConventionChangingUsfmUpdateBlockHandlerTests
{
    [Test]
    public void QuotesSpanningVerses()
    {
        string inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    \v 2 “You shall not eat of any tree of the garden”?»
    ";

        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, \n"
            + "\\v 2 ‘You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SingleEmbed()
    {
        string inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    \f + \ft «This is a “footnote”» \f*
    of the field which Yahweh God had made.
    ";

        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal "
            + "\\f + \\ft “This is a ‘footnote’” \\f* of the field which Yahweh God had made."
        );

        string observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void MultipleEmbeds()
    {
        string inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    \f + \ft «This is a “footnote”» \f*
    of the field \f + \ft Second «footnote» here \f* which Yahweh God had made.
    ";

        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal "
            + "\\f + \\ft “This is a ‘footnote’” \\f* of the field \\f + \\ft Second "
            + "“footnote” here \\f* which Yahweh God had made."
        );

        string observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void QuotesInTextAndEmbed()
    {
        string inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really \f + \ft a
    «footnote» in the «midst of “text”» \f* said,
    “You shall not eat of any tree of the garden”?»
    ";

        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really \\f + \\ft a “footnote” in the “midst of ‘text’” \\f* "
            + "said, ‘You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void QuotesInMultipleVersesAndEmbed()
    {
        string inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God
    \v 2 really \f + \ft a
    «footnote» in the «midst of “text”» \f* said,
    “You shall not eat of any tree of the garden”?»
    ";

        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God\n"
            + "\\v 2 really \\f + \\ft a “footnote” in the “midst of ‘text’” \\f* "
            + "said, ‘You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // Fallback mode does not consider the nesting of quotation marks,
        // but only determines opening/closing marks and maps based on that.
    }

    [Test]
    public void FallbackStrategySameAsFull()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ‘Has God really said,
    “You shall not eat of any tree of the garden”?’
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "british_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackStrategyIncorrectlyNested()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ‘Has God really said,
    ‘You shall not eat of any tree of the garden’?’
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, “You shall not eat of any tree of the garden”?”"
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "british_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackStrategyIncorrectlyNestedSecondCase()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?’
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ‘Has God really said, “You shall not eat of any tree of the garden”?”"
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "british_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackStrategyUnclosedQuote()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ‘Has God really said,
    You shall not eat of any tree of the garden”?’
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "british_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void DefaultQuotationMarkUpdateStrategy()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        You shall not eat of any tree of the garden'?""
    ";
        string expectedFullUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden'?”"
        );

        string expectedBasicUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        string expectedSkippedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, \"Has God really said, You shall not eat of any tree of the garden\'?\""
        );

        string observedUsfm = ChangeQuotationMarks(normalizedUsfm, "typewriter_english", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.ApplyFull)
        );
        AssertUsfmEqual(observedUsfm, expectedFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedBasicUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.Skip)
        );
        AssertUsfmEqual(observedUsfm, expectedSkippedUsfm);
    }

    [Test]
    public void SingleChapterQuotationMarkUpdateStrategy()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        You shall not eat of any tree of the garden'?""
    ";
        string expectedFullUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden'?”"
        );

        string expectedBasicUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        string expectedSkippedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, \"Has God really said, You shall not eat of any tree of the garden\'?\""
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(chapterStrategies: [QuotationMarkUpdateStrategy.ApplyFull])
        );
        AssertUsfmEqual(observedUsfm, expectedFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(chapterStrategies: [QuotationMarkUpdateStrategy.ApplyFallback])
        );
        AssertUsfmEqual(observedUsfm, expectedBasicUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(chapterStrategies: [QuotationMarkUpdateStrategy.Skip])
        );
        AssertUsfmEqual(observedUsfm, expectedSkippedUsfm);
    }

    [Test]
    public void MultipleChapterSameStrategy()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle"" than any animal
        of the field which Yahweh God had made.
    \c 2
    \v 1 He said to the woman, ""Has God really said,
    You shall not eat of any tree of the garden'?""
    ";
        string expectedFullUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle\" than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, “Has God really said, You shall not eat of any tree of the garden'?”"
        );

        string expectedFallbackUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle” than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterStrategies: [QuotationMarkUpdateStrategy.ApplyFull, QuotationMarkUpdateStrategy.ApplyFull]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterStrategies:
                [
                    QuotationMarkUpdateStrategy.ApplyFallback,
                    QuotationMarkUpdateStrategy.ApplyFallback
                ]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFallbackUsfm);
    }

    [Test]
    public void MultipleChapterMultipleStrategies()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle"" than any animal
        of the field which Yahweh God had made.
    \c 2
    \v 1 He said to the woman, ""Has God really said,
    You shall not eat of any tree of the garden'?""
    ";
        string expectedFullThenFallbackUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle\" than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        string expectedFallbackThenFullUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle” than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, “Has God really said, You shall not eat of any tree of the garden'?”"
        );

        string expectedFallbackThenSkipUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle” than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, \"Has God really said, You shall not eat of any tree of the garden\'?\""
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterStrategies: [QuotationMarkUpdateStrategy.ApplyFull, QuotationMarkUpdateStrategy.ApplyFallback]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFullThenFallbackUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterStrategies: [QuotationMarkUpdateStrategy.ApplyFallback, QuotationMarkUpdateStrategy.ApplyFull]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFallbackThenFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterStrategies: [QuotationMarkUpdateStrategy.ApplyFallback, QuotationMarkUpdateStrategy.Skip]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFallbackThenSkipUsfm);
    }

    [Test]
    public void MultiCharacterQuotationMarksInSourceQuoteConvention()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, <<Has God really said,
    <You shall not eat of any tree of the garden>?>>
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_french",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void MultiCharacterQuotationMarksInTargetQuoteConvention()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?”
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, <<Has God really said, <You shall not eat of any tree of the garden>?>>"
        );

        string observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            "typewriter_french",
            new QuotationMarkUpdateSettings(defaultChapterStrategy: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void ProcessScriptureElement()
    {
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "british_english")
        );
        var quotationMarkFinder = new MockQuotationMarkFinder();
        quoteConventionChanger.InternalQuotationMarkFinder = quotationMarkFinder;

        var updateElement = new UsfmUpdateBlockElement(
            UsfmUpdateBlockElementType.Text,
            tokens: [new UsfmToken("test segment")]
        );
        var mockQuotationMarkResolver = new MockQuotationMarkResolver();
        quoteConventionChanger.InternalProcessScriptureElement(updateElement, mockQuotationMarkResolver);

        Assert.That(quotationMarkFinder.NumTimesCalled, Is.EqualTo(1));
        Assert.That(mockQuotationMarkResolver.NumTimesCalled, Is.EqualTo(1));
        Assert.That(quotationMarkFinder.MatchesToReturn[0].TextSegment.Text, Is.EqualTo("this is a ‘test"));
        Assert.That(quotationMarkFinder.MatchesToReturn[1].TextSegment.Text, Is.EqualTo("the test ends” here"));
    }

    [Test]
    public void CreateTextSegmentsBasic()
    {
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        var updateElement = new UsfmUpdateBlockElement(
            UsfmUpdateBlockElementType.Text,
            tokens: [new UsfmToken("test segment")]
        );
        List<TextSegment> textSegments = quoteConventionChanger.InternalCreateTextSegments(updateElement);

        Assert.That(textSegments, Has.Count.EqualTo(1));
        Assert.That(textSegments[0].Text, Is.EqualTo("test segment"));
        Assert.That(textSegments[0].ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.NoMarker));
        Assert.That(textSegments[0].MarkersInPrecedingContext, Has.Count.EqualTo(0));
        Assert.IsNull(textSegments[0].PreviousSegment);
        Assert.IsNull(textSegments[0].NextSegment);
    }

    [Test]
    public void CreateTextSegmentsWithPrecedingMarkers()
    {
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        var updateElement = new UsfmUpdateBlockElement(
            UsfmUpdateBlockElementType.Text,
            tokens:
            [
                new UsfmToken(UsfmTokenType.Verse, null, null, null),
                new UsfmToken(UsfmTokenType.Paragraph, null, null, null),
                new UsfmToken("test segment"),
            ]
        );
        List<TextSegment> textSegments = quoteConventionChanger.InternalCreateTextSegments(updateElement);

        Assert.That(textSegments, Has.Count.EqualTo(1));
        Assert.That(textSegments[0].Text, Is.EqualTo("test segment"));
        Assert.That(textSegments[0].ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.Paragraph));
        Assert.That(
            textSegments[0].MarkersInPrecedingContext.SequenceEqual([UsfmMarkerType.Verse, UsfmMarkerType.Paragraph,])
        );
        Assert.IsNull(textSegments[0].PreviousSegment);
        Assert.IsNull(textSegments[0].NextSegment);
    }

    [Test]
    public void CreateTextSegmentsWithMultipleTextTokens()
    {
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        var updateElement = new UsfmUpdateBlockElement(
            UsfmUpdateBlockElementType.Text,
            tokens:
            [
                new UsfmToken(UsfmTokenType.Verse, null, null, null),
                new UsfmToken(UsfmTokenType.Paragraph, null, null, null),
                new UsfmToken("test segment1"),
                new UsfmToken(UsfmTokenType.Verse, null, null, null),
                new UsfmToken(UsfmTokenType.Character, null, null, null),
                new UsfmToken("test segment2"),
                new UsfmToken(UsfmTokenType.Paragraph, null, null, null),
            ]
        );
        List<TextSegment> textSegments = quoteConventionChanger.InternalCreateTextSegments(updateElement);

        Assert.That(textSegments, Has.Count.EqualTo(2));
        Assert.That(textSegments[0].Text, Is.EqualTo("test segment1"));
        Assert.That(textSegments[0].ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.Paragraph));
        Assert.That(
            textSegments[0].MarkersInPrecedingContext.SequenceEqual([UsfmMarkerType.Verse, UsfmMarkerType.Paragraph,])
        );
        Assert.IsNull(textSegments[0].PreviousSegment);
        Assert.That(textSegments[0].NextSegment, Is.EqualTo(textSegments[1]));
        Assert.That(textSegments[1].Text, Is.EqualTo("test segment2"));
        Assert.That(textSegments[1].ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.Character));
        Assert.That(
            textSegments[1].MarkersInPrecedingContext.SequenceEqual([UsfmMarkerType.Verse, UsfmMarkerType.Character,])
        );
        Assert.That(textSegments[1].PreviousSegment, Is.EqualTo(textSegments[0]));
        Assert.IsNull(textSegments[1].NextSegment);
    }

    [Test]
    public void CreateTextSegment()
    {
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        var usfmToken = new UsfmToken("test segment");
        TextSegment segment = quoteConventionChanger.InternalCreateTextSegment(usfmToken);

        Assert.IsNotNull(segment);
        Assert.That(segment.Text, Is.EqualTo("test segment"));
        Assert.That(segment.ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.NoMarker));
        Assert.That(segment.MarkersInPrecedingContext, Has.Count.EqualTo(0));
        Assert.That(segment.UsfmToken, Is.EqualTo(usfmToken));
    }

    [Test]
    public void SetPreviousAndNextForSegments()
    {
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        List<TextSegment> segments =
        [
            new TextSegment.Builder().SetText("segment 1 text").Build(),
            new TextSegment.Builder().SetText("segment 2 text").Build(),
            new TextSegment.Builder().SetText("segment 3 text").Build()
        ];

        quoteConventionChanger.InternalSetPreviousAndNextForSegments(segments);

        Assert.IsNull(segments[0].PreviousSegment);
        Assert.That(segments[0].NextSegment, Is.EqualTo(segments[1]));
        Assert.That(segments[1].PreviousSegment, Is.EqualTo(segments[0]));
        Assert.That(segments[1].NextSegment, Is.EqualTo(segments[2]));
        Assert.That(segments[2].PreviousSegment, Is.EqualTo(segments[1]));
        Assert.IsNull(segments[2].NextSegment);
    }

    [Test]
    public void UpdateQuotationMarks()
    {
        QuoteConventionChangingUsfmUpdateBlockHandler multiCharToSingleCharQuoteConventionChanger =
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("typewriter_french", "standard_english");

        TextSegment multiCharacterTextSegment = new TextSegment.Builder()
            .SetText("this <<is <a test segment> >>")
            .Build();

        List<QuotationMarkMetadata> multiCharacterQuotationMarks =
        [
            new QuotationMarkMetadata(
                quotationMark: "<<",
                depth: 1,
                direction: QuotationMarkDirection.Opening,
                textSegment: multiCharacterTextSegment,
                startIndex: 5,
                endIndex: 7
            ),
            new QuotationMarkMetadata(
                quotationMark: "<",
                depth: 2,
                direction: QuotationMarkDirection.Opening,
                textSegment: multiCharacterTextSegment,
                startIndex: 10,
                endIndex: 11
            ),
            new QuotationMarkMetadata(
                quotationMark: ">",
                depth: 2,
                direction: QuotationMarkDirection.Closing,
                textSegment: multiCharacterTextSegment,
                startIndex: 25,
                endIndex: 26
            ),
            new QuotationMarkMetadata(
                quotationMark: ">>",
                depth: 1,
                direction: QuotationMarkDirection.Closing,
                textSegment: multiCharacterTextSegment,
                startIndex: 27,
                endIndex: 29
            )
        ];

        multiCharToSingleCharQuoteConventionChanger.UpdateQuotationMarks(multiCharacterQuotationMarks);

        Assert.That(multiCharacterTextSegment.Text, Is.EqualTo("this “is ‘a test segment’ ”"));
        Assert.That(multiCharacterQuotationMarks[0].StartIndex, Is.EqualTo(5));
        Assert.That(multiCharacterQuotationMarks[0].EndIndex, Is.EqualTo(6));
        Assert.That(multiCharacterQuotationMarks[0].TextSegment, Is.EqualTo(multiCharacterTextSegment));
        Assert.That(multiCharacterQuotationMarks[1].StartIndex, Is.EqualTo(9));
        Assert.That(multiCharacterQuotationMarks[1].EndIndex, Is.EqualTo(10));
        Assert.That(multiCharacterQuotationMarks[1].TextSegment, Is.EqualTo(multiCharacterTextSegment));
        Assert.That(multiCharacterQuotationMarks[2].StartIndex, Is.EqualTo(24));
        Assert.That(multiCharacterQuotationMarks[2].EndIndex, Is.EqualTo(25));
        Assert.That(multiCharacterQuotationMarks[2].TextSegment, Is.EqualTo(multiCharacterTextSegment));
        Assert.That(multiCharacterQuotationMarks[3].StartIndex, Is.EqualTo(26));
        Assert.That(multiCharacterQuotationMarks[3].EndIndex, Is.EqualTo(27));
        Assert.That(multiCharacterQuotationMarks[3].TextSegment, Is.EqualTo(multiCharacterTextSegment));

        QuoteConventionChangingUsfmUpdateBlockHandler singleCharToMultiCharQuoteConventionChanger =
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "typewriter_french");

        TextSegment singleCharacterTextSegment = new TextSegment.Builder()
            .SetText("this “is ‘a test segment’ ”")
            .Build();

        List<QuotationMarkMetadata> singleCharacterQuotationMarks =
        [
            new QuotationMarkMetadata(
                quotationMark: "“",
                depth: 1,
                direction: QuotationMarkDirection.Opening,
                textSegment: singleCharacterTextSegment,
                startIndex: 5,
                endIndex: 6
            ),
            new QuotationMarkMetadata(
                quotationMark: "‘",
                depth: 2,
                direction: QuotationMarkDirection.Opening,
                textSegment: singleCharacterTextSegment,
                startIndex: 9,
                endIndex: 10
            ),
            new QuotationMarkMetadata(
                quotationMark: "’",
                depth: 2,
                direction: QuotationMarkDirection.Closing,
                textSegment: singleCharacterTextSegment,
                startIndex: 24,
                endIndex: 25
            ),
            new QuotationMarkMetadata(
                quotationMark: "”",
                depth: 1,
                direction: QuotationMarkDirection.Closing,
                textSegment: singleCharacterTextSegment,
                startIndex: 26,
                endIndex: 27
            )
        ];

        singleCharToMultiCharQuoteConventionChanger.UpdateQuotationMarks(singleCharacterQuotationMarks);

        Assert.That(singleCharacterTextSegment.Text, Is.EqualTo("this <<is <a test segment> >>"));
        Assert.That(singleCharacterQuotationMarks[0].StartIndex, Is.EqualTo(5));
        Assert.That(singleCharacterQuotationMarks[0].EndIndex, Is.EqualTo(7));
        Assert.That(singleCharacterQuotationMarks[0].TextSegment, Is.EqualTo(singleCharacterTextSegment));
        Assert.That(singleCharacterQuotationMarks[1].StartIndex, Is.EqualTo(10));
        Assert.That(singleCharacterQuotationMarks[1].EndIndex, Is.EqualTo(11));
        Assert.That(singleCharacterQuotationMarks[1].TextSegment, Is.EqualTo(singleCharacterTextSegment));
        Assert.That(singleCharacterQuotationMarks[2].StartIndex, Is.EqualTo(25));
        Assert.That(singleCharacterQuotationMarks[2].EndIndex, Is.EqualTo(26));
        Assert.That(singleCharacterQuotationMarks[2].TextSegment, Is.EqualTo(singleCharacterTextSegment));
        Assert.That(singleCharacterQuotationMarks[3].StartIndex, Is.EqualTo(27));
        Assert.That(singleCharacterQuotationMarks[3].EndIndex, Is.EqualTo(29));
        Assert.That(singleCharacterQuotationMarks[3].TextSegment, Is.EqualTo(singleCharacterTextSegment));
    }

    [Test]
    public void CheckForChapterChange()
    {
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        Assert.That(quoteConventionChanger.InternalCurrentChapterNumber, Is.EqualTo(0));

        quoteConventionChanger.InternalCheckForChapterChange(new UsfmUpdateBlock([ScriptureRef.Parse("MAT 1:1")], []));

        Assert.That(quoteConventionChanger.InternalCurrentChapterNumber, Is.EqualTo(1));

        quoteConventionChanger.InternalCheckForChapterChange(
            new UsfmUpdateBlock([ScriptureRef.Parse("ISA 15:22")], [])
        );

        Assert.That(quoteConventionChanger.InternalCurrentChapterNumber, Is.EqualTo(15));
    }

    [Test]
    public void StartNewChapter()
    {
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler(
                "standard_english",
                "standard_english",
                new QuotationMarkUpdateSettings(
                    chapterStrategies:
                    [
                        QuotationMarkUpdateStrategy.Skip,
                        QuotationMarkUpdateStrategy.ApplyFull,
                        QuotationMarkUpdateStrategy.ApplyFallback,
                    ]
                )
            )
        );

        quoteConventionChanger.InternalVerseTextQuotationMarkResolver = new MockQuotationMarkResolver();

        quoteConventionChanger
            .InternalNextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Embed)
            .SetText("this text should be erased");
        quoteConventionChanger.InternalVerseTextQuotationMarkResolver.InternalIssues.Add(
            QuotationMarkResolutionIssue.IncompatibleQuotationMark
        );

        quoteConventionChanger.InternalStartNewChapter(1);
        TextSegment segment = quoteConventionChanger.InternalNextScriptureTextSegmentBuilder.Build();
        Assert.That(quoteConventionChanger.InternalCurrentStrategy, Is.EqualTo(QuotationMarkUpdateStrategy.Skip));
        Assert.That(segment.ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.Chapter));
        Assert.That(segment.Text, Is.EqualTo(""));
        Assert.That(!segment.MarkersInPrecedingContext.Contains(UsfmMarkerType.Embed));
        Assert.That(quoteConventionChanger.InternalVerseTextQuotationMarkResolver.InternalIssues, Has.Count.EqualTo(0));

        quoteConventionChanger.InternalStartNewChapter(2);
        Assert.That(quoteConventionChanger.InternalCurrentStrategy, Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFull));

        quoteConventionChanger.InternalStartNewChapter(3);
        Assert.That(
            quoteConventionChanger.InternalCurrentStrategy,
            Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFallback)
        );
    }

    private static string ChangeQuotationMarks(
        string normalizedUsfm,
        string sourceQuoteConventionName,
        string targetQuoteConventionName,
        QuotationMarkUpdateSettings? quotationMarkUpdateSettings = null
    )
    {
        quotationMarkUpdateSettings ??= new QuotationMarkUpdateSettings();
        MockQuoteConventionChangingUsfmUpdateBlockHandler quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler(
                sourceQuoteConventionName,
                targetQuoteConventionName,
                quotationMarkUpdateSettings
            )
        );

        var updater = new UpdateUsfmParserHandler(updateBlockHandlers: [quoteConventionChanger]);
        UsfmParser.Parse(normalizedUsfm, updater);

        return updater.GetUsfm();
    }

    private static MockQuoteConventionChangingUsfmUpdateBlockHandler CreateQuoteConventionChangingUsfmUpdateBlockHandler(
        string sourceQuoteConventionName,
        string targetQuoteConventionName,
        QuotationMarkUpdateSettings? quotationMarkUpdateSettings = null
    )
    {
        quotationMarkUpdateSettings ??= new QuotationMarkUpdateSettings();
        QuoteConvention sourceQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
            sourceQuoteConventionName
        );
        Assert.IsNotNull(sourceQuoteConvention);

        QuoteConvention targetQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
            targetQuoteConventionName
        );
        Assert.IsNotNull(targetQuoteConvention);

        return new MockQuoteConventionChangingUsfmUpdateBlockHandler(
            sourceQuoteConvention,
            targetQuoteConvention,
            quotationMarkUpdateSettings
        );
    }

    private static void AssertUsfmEqual(string observedUsfm, string expectedUsfm)
    {
        foreach ((string observedLine, string expectedLine) in observedUsfm.Split("\n").Zip(expectedUsfm.Split("\n")))
            Assert.That(observedLine.Trim(), Is.EqualTo(expectedLine.Trim()));
    }

    private class MockQuoteConventionChangingUsfmUpdateBlockHandler(
        QuoteConvention sourceQuoteConvention,
        QuoteConvention targetQuoteConvention,
        QuotationMarkUpdateSettings settings
    ) : QuoteConventionChangingUsfmUpdateBlockHandler(sourceQuoteConvention, targetQuoteConvention, settings)
    {
        public QuotationMarkFinder InternalQuotationMarkFinder
        {
            set => QuotationMarkFinder = value;
        }

        public TextSegment.Builder InternalNextScriptureTextSegmentBuilder
        {
            get => NextScriptureTextSegmentBuilder;
        }
        public MockQuotationMarkResolver InternalVerseTextQuotationMarkResolver
        {
            get =>
                VerseTextQuotationMarkResolver is MockQuotationMarkResolver mqmr
                    ? mqmr
                    : throw new InvalidOperationException(
                        "Unable to use implementations of IQuotationMarkResolver other than MockQuotationMarkResolver"
                    );
            set => VerseTextQuotationMarkResolver = value;
        }
        public int InternalCurrentChapterNumber
        {
            get => CurrentChapterNumber;
            set => CurrentChapterNumber = value;
        }
        public QuotationMarkUpdateStrategy InternalCurrentStrategy
        {
            get => CurrentStrategy;
            set => CurrentStrategy = value;
        }

        public void InternalProcessScriptureElement(
            UsfmUpdateBlockElement element,
            IQuotationMarkResolver quotationMarkResolver
        )
        {
            ProcessScriptureElement(element, quotationMarkResolver);
        }

        public List<TextSegment> InternalCreateTextSegments(UsfmUpdateBlockElement element)
        {
            return CreateTextSegments(element);
        }

        public TextSegment InternalCreateTextSegment(UsfmToken usfmToken)
        {
            return CreateTextSegment(usfmToken);
        }

        public List<TextSegment> InternalSetPreviousAndNextForSegments(List<TextSegment> textSegments)
        {
            return SetPreviousAndNextForSegments(textSegments);
        }

        public void InternalStartNewChapter(int newChapterNum)
        {
            StartNewChapter(newChapterNum);
        }

        public void InternalCheckForChapterChange(UsfmUpdateBlock block)
        {
            CheckForChapterChange(block);
        }
    }

    private class MockQuotationMarkFinder : QuotationMarkFinder
    {
        public int NumTimesCalled;
        public readonly List<QuotationMarkStringMatch> MatchesToReturn;

        public MockQuotationMarkFinder()
            : base(new QuoteConventionSet([]))
        {
            NumTimesCalled = 0;
            MatchesToReturn =
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("this is a \"test").Build(), 10, 11),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("the test ends\" here").Build(), 13, 14),
            ];
        }

        public override List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInTextSegments(
            IReadOnlyList<TextSegment> textSegments
        )
        {
            NumTimesCalled++;
            return MatchesToReturn;
        }
    }

    private class MockQuotationMarkResolver(IQuotationMarkResolutionSettings? settings = null)
        : DepthBasedQuotationMarkResolver(
            settings ?? new QuoteConventionDetectionResolutionSettings(new QuoteConventionSet([]))
        )
    {
        public int NumTimesCalled = 0;

        public HashSet<QuotationMarkResolutionIssue> InternalIssues => Issues;

        public override void Reset()
        {
            base.Reset();
            NumTimesCalled = 0;
        }

        public override IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(
            IReadOnlyList<QuotationMarkStringMatch> quoteMatches
        )
        {
            NumTimesCalled++;
            int currentDepth = 1;
            QuotationMarkDirection currentDirection = QuotationMarkDirection.Opening;
            foreach (QuotationMarkStringMatch quoteMatch in quoteMatches)
            {
                yield return quoteMatch.Resolve(currentDepth, currentDirection);
                currentDepth++;
                currentDirection =
                    currentDirection == QuotationMarkDirection.Opening
                        ? QuotationMarkDirection.Closing
                        : QuotationMarkDirection.Opening;
            }
        }

        public override HashSet<QuotationMarkResolutionIssue> GetIssues()
        {
            return new HashSet<QuotationMarkResolutionIssue>();
        }
    }
}
