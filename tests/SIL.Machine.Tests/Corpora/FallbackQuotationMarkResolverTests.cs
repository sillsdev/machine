using NUnit.Framework;
using SIL.Machine.PunctuationAnalysis;

namespace SIL.Machine.Corpora;

[TestFixture]
public class FallbackQuotationMarkResolverTests
{
    [Test]
    public void Reset()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention)
        );

        basicQuotationMarkResolver.LastQuotationMark = new QuotationMarkMetadata(
            "\"",
            1,
            QuotationMarkDirection.Opening,
            new TextSegment.Builder().SetText("\"'test text\"").Build(),
            0,
            1
        );
        basicQuotationMarkResolver.Issues.Add(QuotationMarkResolutionIssue.UnexpectedQuotationMark);

        basicQuotationMarkResolver.Reset();
        Assert.IsNull(basicQuotationMarkResolver.LastQuotationMark);
        Assert.That(basicQuotationMarkResolver.Issues.Count, Is.EqualTo(0));
    }

    [Test]
    public void SimpleQuotationMarkResolutionWithNoPreviousMark()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention.Normalize())
        );

        var actualResolvedQuotationMarks = basicQuotationMarkResolver
            .ResolveQuotationMarks(
                [new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test \" text").Build(), 5, 6),]
            )
            .ToList();
        List<QuotationMarkMetadata> expectedResolvedQuotationMarks =
        [
            new QuotationMarkMetadata(
                "\"",
                1,
                QuotationMarkDirection.Opening,
                new TextSegment.Builder().SetText("test \" text").Build(),
                5,
                6
            )
        ];

        AssertResolvedQuotationMarksEqual(actualResolvedQuotationMarks, expectedResolvedQuotationMarks);
    }

    [Test]
    public void SimpleQuotationMarkResolutionWithPreviousOpeningMark()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention.Normalize())
        );

        var actualResolvedQuotationMarks = basicQuotationMarkResolver
            .ResolveQuotationMarks(
                [
                    new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test \" text").Build(), 0, 1),
                    new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test \" text").Build(), 6, 7),
                ]
            )
            .ToList();
        List<QuotationMarkMetadata> expectedResolvedQuotationMarks =
        [
            new QuotationMarkMetadata(
                "\"",
                1,
                QuotationMarkDirection.Opening,
                new TextSegment.Builder().SetText("\"test \" text").Build(),
                0,
                1
            ),
            new QuotationMarkMetadata(
                "\"",
                1,
                QuotationMarkDirection.Closing,
                new TextSegment.Builder().SetText("\"test \" text").Build(),
                6,
                7
            ),
        ];

        AssertResolvedQuotationMarksEqual(actualResolvedQuotationMarks, expectedResolvedQuotationMarks);
    }

    [Test]
    public void SimpleQuotationMarkResolutionWithPreviousClosingMark()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention.Normalize())
        );

        var actualResolvedQuotationMarks = basicQuotationMarkResolver
            .ResolveQuotationMarks(
                [
                    new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test\" \" text").Build(), 4, 5),
                    new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test\" \" text").Build(), 6, 7),
                ]
            )
            .ToList();
        List<QuotationMarkMetadata> expectedResolvedQuotationMarks =
        [
            new QuotationMarkMetadata(
                "\"",
                1,
                QuotationMarkDirection.Closing,
                new TextSegment.Builder().SetText("test\" \" text").Build(),
                4,
                5
            ),
            new QuotationMarkMetadata(
                "\"",
                1,
                QuotationMarkDirection.Opening,
                new TextSegment.Builder().SetText("test\" \" text").Build(),
                6,
                7
            )
        ];

        AssertResolvedQuotationMarksEqual(actualResolvedQuotationMarks, expectedResolvedQuotationMarks);
    }

    [Test]
    public void IsOpeningQuote()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention.Normalize())
        );

        // valid opening quote at start of segment
        var quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test text\"").Build(), 0, 1);
        Assert.IsTrue(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // opening quote with leading whitespace
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test \"text\"").Build(), 5, 6);
        Assert.IsTrue(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // opening quote with quote introducer
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test:\"text\"").Build(), 5, 6);
        Assert.IsTrue(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // QuotationMarkStringMatch indices don't indicate a quotation mark
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test \"text\"").Build(), 0, 1);
        Assert.IsFalse(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // the quotation mark is not valid under the current quote convention
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("<test \"text\"").Build(), 0, 1);
        Assert.IsFalse(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // no leading whitespace before quotation mark
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test\"text\"").Build(), 4, 5);
        Assert.IsFalse(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // closing quote at the end of the segment
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test text\"").Build(), 10, 11);
        Assert.IsFalse(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // closing quote with trailing whitespace
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test text\" ").Build(), 10, 11);
        Assert.IsFalse(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));
    }

    [Test]
    public void IsOpeningQuoteWithUnambiguousQuoteConvention()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuoteConventionDetectionResolutionSettings(new QuoteConventionSet([englishQuoteConvention]))
        );

        // unambiguous opening quote at start of segment
        var quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("“test text”").Build(), 0, 1);
        Assert.IsTrue(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // unambiguous opening quote with leading whitespace
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test “text”").Build(), 5, 6);
        Assert.IsTrue(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // unambiguous opening quote without the "correct" context
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test“text”").Build(), 4, 5);
        Assert.IsTrue(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // unambiguous closing quote
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("“test” text").Build(), 5, 6);
        Assert.IsFalse(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));
    }

    [Test]
    public void IsOpeningQuoteStateful()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention.Normalize())
        );

        // no preceding quote
        var quoteMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\"'test text\"").Build(),
            1,
            2
        );
        Assert.IsFalse(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));

        // immediately preceding quote
        basicQuotationMarkResolver.LastQuotationMark = new QuotationMarkMetadata(
            "\"",
            1,
            QuotationMarkDirection.Opening,
            new TextSegment.Builder().SetText("\"'test text\"").Build(),
            0,
            1
        );
        Assert.IsTrue(basicQuotationMarkResolver.IsOpeningQuotationMark(quoteMatch));
    }

    [Test]
    public void DoesMostRecentOpeningMarkImmediatelyPrecede()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention)
        );

        // no preceding quote
        var nestedQuoteMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\"'test text\"").Build(),
            1,
            2
        );
        Assert.IsFalse(basicQuotationMarkResolver.DoesMostRecentOpeningMarkImmediatelyPrecede(nestedQuoteMatch));

        // correct preceding quote
        basicQuotationMarkResolver.LastQuotationMark = new QuotationMarkMetadata(
            "\"",
            1,
            QuotationMarkDirection.Opening,
            new TextSegment.Builder().SetText("\"'test text\"").Build(),
            0,
            1
        );
        Assert.IsTrue(basicQuotationMarkResolver.DoesMostRecentOpeningMarkImmediatelyPrecede(nestedQuoteMatch));

        // wrong direction for preceding quote
        basicQuotationMarkResolver.LastQuotationMark = new QuotationMarkMetadata(
            "\"",
            1,
            QuotationMarkDirection.Closing,
            new TextSegment.Builder().SetText("\"'test text\"").Build(),
            0,
            1
        );
        Assert.IsFalse(basicQuotationMarkResolver.DoesMostRecentOpeningMarkImmediatelyPrecede(nestedQuoteMatch));

        // different text segment for preceding quote
        basicQuotationMarkResolver.LastQuotationMark = new QuotationMarkMetadata(
            "\"",
            1,
            QuotationMarkDirection.Opening,
            new TextSegment.Builder().SetText("\"'different text\"").Build(),
            0,
            1
        );
        Assert.IsFalse(basicQuotationMarkResolver.DoesMostRecentOpeningMarkImmediatelyPrecede(nestedQuoteMatch));

        // previous quote is not *immediately* before the current quote
        nestedQuoteMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\" 'test text\"").Build(),
            2,
            3
        );
        basicQuotationMarkResolver.LastQuotationMark = new QuotationMarkMetadata(
            "\"",
            1,
            QuotationMarkDirection.Opening,
            new TextSegment.Builder().SetText("\" 'test text\"").Build(),
            0,
            1
        );
        Assert.IsFalse(basicQuotationMarkResolver.DoesMostRecentOpeningMarkImmediatelyPrecede(nestedQuoteMatch));
    }

    [Test]
    public void IsClosingQuote()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention.Normalize())
        );

        // valid closing quote at end of segment
        var quoteMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\"test text\"").Build(),
            10,
            11
        );
        Assert.IsTrue(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // closing quote with trailing whitespace
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test\" text").Build(), 5, 6);
        Assert.IsTrue(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // closing quote with trailing punctuation
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test text\".").Build(), 10, 11);
        Assert.IsTrue(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // QuotationMarkStringMatch indices don't indicate a quotation mark
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test text\"").Build(), 9, 10);
        Assert.IsFalse(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // the quotation mark is not valid under the current quote convention
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test \"text>").Build(), 10, 11);
        Assert.IsFalse(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // no trailing whitespace after quotation mark
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test\"text").Build(), 5, 6);
        Assert.IsFalse(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // opening quote at the start of the segment
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test text\"").Build(), 0, 1);
        Assert.IsFalse(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // opening quote with leading whitespace
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test \"text\"").Build(), 5, 6);
        Assert.IsFalse(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));
    }

    [Test]
    public void IsClosingQuoteWithUnambiguousQuoteConvention()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuoteConventionDetectionResolutionSettings(new QuoteConventionSet([englishQuoteConvention]))
        );

        // unambiguous closing quote at end of segment
        var quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("“test text”").Build(), 10, 11);
        Assert.IsTrue(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // unambiguous closing quote with trailing whitespace
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("“test” text").Build(), 5, 6);
        Assert.IsTrue(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // unambiguous closing quote without the "correct" context
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("“test”text").Build(), 5, 6);
        Assert.IsTrue(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));

        // unambiguous opening quote
        quoteMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("test “text”").Build(), 5, 6);
        Assert.IsFalse(basicQuotationMarkResolver.IsClosingQuotationMark(quoteMatch));
    }

    [Test]
    public void ResolveOpeningQuote()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention.Normalize())
        );

        var expectedResolvedQuotationMark = new QuotationMarkMetadata(
            "\"",
            1,
            QuotationMarkDirection.Opening,
            new TextSegment.Builder().SetText("\"test text\"").Build(),
            0,
            1
        );
        var actualResolvedQuotationMark = basicQuotationMarkResolver.ResolveOpeningMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test text\"").Build(), 0, 1)
        );
        Assert.That(actualResolvedQuotationMark, Is.EqualTo(expectedResolvedQuotationMark));
        Assert.That(basicQuotationMarkResolver.LastQuotationMark, Is.EqualTo(actualResolvedQuotationMark));
    }

    [Test]
    public void ResolveClosingQuote()
    {
        var englishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.IsNotNull(englishQuoteConvention);

        var basicQuotationMarkResolver = new FallbackQuotationMarkResolver(
            new QuotationMarkUpdateResolutionSettings(englishQuoteConvention.Normalize())
        );

        var expectedResolvedQuotationMark = new QuotationMarkMetadata(
            "\"",
            1,
            QuotationMarkDirection.Closing,
            new TextSegment.Builder().SetText("\"test text\"").Build(),
            10,
            11
        );
        var actualResolvedQuotationMark = basicQuotationMarkResolver.ResolveClosingMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"test text\"").Build(), 10, 11)
        );
        Assert.That(actualResolvedQuotationMark, Is.EqualTo(expectedResolvedQuotationMark));
    }

    public void AssertResolvedQuotationMarksEqual(
        List<QuotationMarkMetadata> actualResolvedQuotationMarks,
        List<QuotationMarkMetadata> expectedResolvedQuotationMarks
    )
    {
        Assert.That(actualResolvedQuotationMarks.Count, Is.EqualTo(expectedResolvedQuotationMarks.Count));
        foreach (
            (QuotationMarkMetadata actualMark, QuotationMarkMetadata expectedMark) in actualResolvedQuotationMarks.Zip(
                expectedResolvedQuotationMarks
            )
        )
        {
            Assert.That(actualMark, Is.EqualTo(expectedMark));
        }
    }
}
