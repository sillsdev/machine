using NUnit.Framework;
using SIL.Machine.Corpora.PunctuationAnalysis;

namespace SIL.Machine.Corpora;

[TestFixture]
public class QuoteConventionChangingUsfmUpdateBlockHandlerTests
{
    [Test]
    public void QuotesSpanningVerses()
    {
        var inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    \v 2 “You shall not eat of any tree of the garden”?»
    ";

        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, \n"
            + "\\v 2 ‘You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SingleEmbed()
    {
        var inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    \f + \ft «This is a “footnote”» \f*
    of the field which Yahweh God had made.
    ";

        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal "
            + "\\f + \\ft “This is a ‘footnote’” \\f* of the field which Yahweh God had made."
        );

        var observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void MultipleEmbeds()
    {
        var inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    \f + \ft «This is a “footnote”» \f*
    of the field \f + \ft Second «footnote» here \f* which Yahweh God had made.
    ";

        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal "
            + "\\f + \\ft “This is a ‘footnote’” \\f* of the field \\f + \\ft Second "
            + "“footnote” here \\f* which Yahweh God had made."
        );

        var observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void QuotesInTextAndEmbed()
    {
        var inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really \f + \ft a
    «footnote» in the «midst of “text”» \f* said,
    “You shall not eat of any tree of the garden”?»
    ";

        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really \\f + \\ft a “footnote” in the “midst of ‘text’” \\f* "
            + "said, ‘You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void QuotesInMultipleVersesAndEmbed()
    {
        var inputUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God
    \v 2 really \f + \ft a
    «footnote» in the «midst of “text”» \f* said,
    “You shall not eat of any tree of the garden”?»
    ";

        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God\n"
            + "\\v 2 really \\f + \\ft a “footnote” in the “midst of ‘text’” \\f* "
            + "said, ‘You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = ChangeQuotationMarks(inputUsfm, "western_european", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // Fallback mode does not consider the nesting of quotation marks,
        // but only determines opening/closing marks and maps based on that.
    }

    [Test]
    public void FallbackStrategySameAsFull()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ‘Has God really said,
    “You shall not eat of any tree of the garden”?’
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "british_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterAction: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackStrategyIncorrectlyNested()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ‘Has God really said,
    ‘You shall not eat of any tree of the garden’?’
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, “You shall not eat of any tree of the garden”?”"
        );

        var observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "british_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterAction: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackStrategyIncorrectlyNestedSecondCase()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?’
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ‘Has God really said, “You shall not eat of any tree of the garden”?”"
        );

        var observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "british_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterAction: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackStrategyUnclosedQuote()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ‘Has God really said,
    You shall not eat of any tree of the garden”?’
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "british_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterAction: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void DefaultQuotationMarkUpdateStrategy()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        You shall not eat of any tree of the garden'?""
    ";
        var expectedFullUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden'?”"
        );

        var expectedBasicUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        var expectedSkippedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, \"Has God really said, You shall not eat of any tree of the garden\'?\""
        );

        var observedUsfm = ChangeQuotationMarks(normalizedUsfm, "typewriter_english", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterAction: QuotationMarkUpdateStrategy.ApplyFull)
        );
        AssertUsfmEqual(observedUsfm, expectedFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterAction: QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedBasicUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(defaultChapterAction: QuotationMarkUpdateStrategy.Skip)
        );
        AssertUsfmEqual(observedUsfm, expectedSkippedUsfm);
    }

    [Test]
    public void SingleChapterQuotationMarkUpdateStrategy()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        You shall not eat of any tree of the garden'?""
    ";
        var expectedFullUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden'?”"
        );

        var expectedBasicUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        var expectedSkippedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, \"Has God really said, You shall not eat of any tree of the garden\'?\""
        );

        var observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(chapterActions: [QuotationMarkUpdateStrategy.ApplyFull])
        );
        AssertUsfmEqual(observedUsfm, expectedFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(chapterActions: [QuotationMarkUpdateStrategy.ApplyFallback])
        );
        AssertUsfmEqual(observedUsfm, expectedBasicUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(chapterActions: [QuotationMarkUpdateStrategy.Skip])
        );
        AssertUsfmEqual(observedUsfm, expectedSkippedUsfm);
    }

    [Test]
    public void MultipleChapterSameStrategy()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle"" than any animal
        of the field which Yahweh God had made.
    \c 2
    \v 1 He said to the woman, ""Has God really said,
    You shall not eat of any tree of the garden'?""
    ";
        var expectedFullUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle\" than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, “Has God really said, You shall not eat of any tree of the garden'?”"
        );

        var expectedFallbackUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle” than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterActions: [QuotationMarkUpdateStrategy.ApplyFull, QuotationMarkUpdateStrategy.ApplyFull]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterActions: [QuotationMarkUpdateStrategy.ApplyFallback, QuotationMarkUpdateStrategy.ApplyFallback]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFallbackUsfm);
    }

    [Test]
    public void MultipleChapterMultipleStrategies()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle"" than any animal
        of the field which Yahweh God had made.
    \c 2
    \v 1 He said to the woman, ""Has God really said,
    You shall not eat of any tree of the garden'?""
    ";
        var expectedFullThenFallbackUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle\" than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        var expectedFallbackThenFullUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle” than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, “Has God really said, You shall not eat of any tree of the garden'?”"
        );

        var expectedFallbackThenSkipUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle” than any animal of the field which Yahweh God had made.\n"
            + "\\c 2\n"
            + "\\v 1 He said to the woman, \"Has God really said, You shall not eat of any tree of the garden\'?\""
        );

        var observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterActions: [QuotationMarkUpdateStrategy.ApplyFull, QuotationMarkUpdateStrategy.ApplyFallback]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFullThenFallbackUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterActions: [QuotationMarkUpdateStrategy.ApplyFallback, QuotationMarkUpdateStrategy.ApplyFull]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFallbackThenFullUsfm);

        observedUsfm = ChangeQuotationMarks(
            normalizedUsfm,
            "typewriter_english",
            "standard_english",
            new QuotationMarkUpdateSettings(
                chapterActions: [QuotationMarkUpdateStrategy.ApplyFallback, QuotationMarkUpdateStrategy.Skip]
            )
        );
        AssertUsfmEqual(observedUsfm, expectedFallbackThenSkipUsfm);
    }

    [Test]
    public void ProcessScriptureElement()
    {
        var quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "british_english")
        );
        var quotationMarkFinder = new MockQuotationMarkFinder();
        quoteConventionChanger.QuotationMarkFinder = quotationMarkFinder;

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
        var quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        var updateElement = new UsfmUpdateBlockElement(
            UsfmUpdateBlockElementType.Text,
            tokens: [new UsfmToken("test segment")]
        );
        var textSegments = quoteConventionChanger.InternalCreateTextSegments(updateElement);

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
        var quoteConventionChanger = (
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
        var textSegments = quoteConventionChanger.InternalCreateTextSegments(updateElement);

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
        var quoteConventionChanger = (
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
        var textSegments = quoteConventionChanger.InternalCreateTextSegments(updateElement);

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
        var quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        var usfmToken = new UsfmToken("test segment");
        var segment = quoteConventionChanger.InternalCreateTextSegment(usfmToken);

        Assert.IsNotNull(segment);
        Assert.That(segment.Text, Is.EqualTo("test segment"));
        Assert.That(segment.ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.NoMarker));
        Assert.That(segment.MarkersInPrecedingContext, Has.Count.EqualTo(0));
        Assert.That(segment.UsfmToken, Is.EqualTo(usfmToken));
    }

    [Test]
    public void SetPreviousAndNextForSegments()
    {
        var quoteConventionChanger = (
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
    public void CheckForChapterChange()
    {
        var quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler("standard_english", "standard_english")
        );

        Assert.That(quoteConventionChanger.CurrentChapterNumber, Is.EqualTo(0));

        quoteConventionChanger.InternalCheckForChapterChange(new UsfmUpdateBlock([ScriptureRef.Parse("MAT 1:1")], []));

        Assert.That(quoteConventionChanger.CurrentChapterNumber, Is.EqualTo(1));

        quoteConventionChanger.InternalCheckForChapterChange(
            new UsfmUpdateBlock([ScriptureRef.Parse("ISA 15:22")], [])
        );

        Assert.That(quoteConventionChanger.CurrentChapterNumber, Is.EqualTo(15));
    }

    [Test]
    public void StartNewChapter()
    {
        var quoteConventionChanger = (
            CreateQuoteConventionChangingUsfmUpdateBlockHandler(
                "standard_english",
                "standard_english",
                new QuotationMarkUpdateSettings(
                    chapterActions:
                    [
                        QuotationMarkUpdateStrategy.Skip,
                        QuotationMarkUpdateStrategy.ApplyFull,
                        QuotationMarkUpdateStrategy.ApplyFallback,
                    ]
                )
            )
        );

        quoteConventionChanger.VerseTextQuotationMarkResolver = new MockQuotationMarkResolver();

        quoteConventionChanger
            .NextScriptureTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Embed)
            .SetText("this text should be erased");
        quoteConventionChanger.VerseTextQuotationMarkResolver.InternalIssues.Add(
            QuotationMarkResolutionIssue.IncompatibleQuotationMark
        );

        quoteConventionChanger.InternalStartNewChapter(1);
        var segment = quoteConventionChanger.NextScriptureTextSegmentBuilder.Build();
        Assert.That(quoteConventionChanger.CurrentStrategy, Is.EqualTo(QuotationMarkUpdateStrategy.Skip));
        Assert.That(segment.ImmediatePrecedingMarker, Is.EqualTo(UsfmMarkerType.Chapter));
        Assert.That(segment.Text, Is.EqualTo(""));
        Assert.That(!segment.MarkersInPrecedingContext.Contains(UsfmMarkerType.Embed));
        Assert.That(quoteConventionChanger.VerseTextQuotationMarkResolver.InternalIssues, Has.Count.EqualTo(0));

        quoteConventionChanger.InternalStartNewChapter(2);
        Assert.That(quoteConventionChanger.CurrentStrategy, Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFull));

        quoteConventionChanger.InternalStartNewChapter(3);
        Assert.That(quoteConventionChanger.CurrentStrategy, Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFallback));
    }

    private static string ChangeQuotationMarks(
        string normalizedUsfm,
        string sourceQuoteConventionName,
        string targetQuoteConventionName,
        QuotationMarkUpdateSettings? quotationMarkUpdateSettings = null
    )
    {
        quotationMarkUpdateSettings ??= new QuotationMarkUpdateSettings();
        var quoteConventionChanger = (
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
        var sourceQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            sourceQuoteConventionName
        );
        Assert.IsNotNull(sourceQuoteConvention);

        var targetQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
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
        public QuotationMarkFinder QuotationMarkFinder
        {
            set => _quotationMarkFinder = value;
        }

        public TextSegment.Builder NextScriptureTextSegmentBuilder
        {
            get => _nextScriptureTextSegmentBuilder;
        }
        public MockQuotationMarkResolver VerseTextQuotationMarkResolver
        {
            get =>
                _verseTextQuotationMarkResolver is MockQuotationMarkResolver mqmr
                    ? mqmr
                    : throw new InvalidOperationException(
                        "Unable to use implementations of IQuotationMarkResolver other than MockQuotationMarkResolver"
                    );
            set => _verseTextQuotationMarkResolver = value;
        }
        public int CurrentChapterNumber
        {
            get => _currentChapterNumber;
            set => _currentChapterNumber = value;
        }
        public QuotationMarkUpdateStrategy CurrentStrategy
        {
            get => _currentStrategy;
            set => _currentStrategy = value;
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
            List<TextSegment> textSegments
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
            List<QuotationMarkStringMatch> quoteMatches
        )
        {
            NumTimesCalled++;
            int currentDepth = 1;
            var currentDirection = QuotationMarkDirection.Opening;
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
