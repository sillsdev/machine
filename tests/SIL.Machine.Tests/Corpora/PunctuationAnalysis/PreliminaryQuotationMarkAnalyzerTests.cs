using NUnit.Framework;

namespace SIL.Machine.Corpora.PunctuationAnalysis;

[TestFixture]
public class PreliminaryQuotationMarkAnalyzerTests
{
    // ApostropheProportionStatistics tests
    [Test]
    public void ApostropheProportionStatisticsReset()
    {
        var apostropheProportionStatistics = new ApostropheProportionStatistics();
        apostropheProportionStatistics.CountCharacters(new TextSegment.Builder().SetText("'").Build());
        apostropheProportionStatistics.AddApostrophe();
        Assert.IsTrue(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.5));

        apostropheProportionStatistics.Reset();
        Assert.IsFalse(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.5));
    }

    [Test]
    public void IsApostropheProportionGreaterThan()
    {
        var apostropheProportionStatistics = new ApostropheProportionStatistics();
        Assert.IsFalse(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.0));

        // invalid case where no characters have been counted
        apostropheProportionStatistics.AddApostrophe();
        Assert.IsFalse(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.0));

        apostropheProportionStatistics.CountCharacters(new TextSegment.Builder().SetText("a").Build());
        Assert.IsTrue(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.99));

        apostropheProportionStatistics.AddApostrophe();
        apostropheProportionStatistics.CountCharacters(new TextSegment.Builder().SetText("bcd").Build());
        Assert.IsTrue(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.4));
        Assert.IsFalse(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.5));

        apostropheProportionStatistics.CountCharacters(new TextSegment.Builder().SetText("ef").Build());
        Assert.IsTrue(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.3));
        Assert.IsFalse(apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.4));

        // QuotationMarkWordPosition tests
    }

    [Test]
    public void IsMarkRarelyInitial()
    {
        var quotationMarkWordPositions = new QuotationMarkWordPositions();
        Assert.IsFalse(quotationMarkWordPositions.IsMarkRarelyInitial("\u201d"));

        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkRarelyInitial("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        Assert.IsFalse(quotationMarkWordPositions.IsMarkRarelyInitial("\u201d"));

        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkRarelyInitial("\u201d"));

        quotationMarkWordPositions.CountWordFinalApostrophe("\u201c");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkRarelyInitial("\u201d"));

        quotationMarkWordPositions.CountWordFinalApostrophe("\u201c");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201c");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkRarelyInitial("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        Assert.IsFalse(quotationMarkWordPositions.IsMarkRarelyInitial("\u201d"));
    }

    [Test]
    public void IsMarkRarelyFinal()
    {
        var quotationMarkWordPositions = new QuotationMarkWordPositions();
        Assert.IsFalse(quotationMarkWordPositions.IsMarkRarelyFinal("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkRarelyFinal("\u201d"));

        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        Assert.IsFalse(quotationMarkWordPositions.IsMarkRarelyFinal("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkRarelyFinal("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201c");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkRarelyFinal("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201c");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201c");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkRarelyFinal("\u201d"));

        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        Assert.IsFalse(quotationMarkWordPositions.IsMarkRarelyFinal("\u201d"));
    }

    [Test]
    public void AreInitialAndFinalRatesSimilar()
    {
        var quotationMarkWordPositions = new QuotationMarkWordPositions();
        Assert.IsFalse(quotationMarkWordPositions.AreInitialAndFinalRatesSimilar("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        Assert.IsTrue(quotationMarkWordPositions.AreInitialAndFinalRatesSimilar("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        Assert.IsFalse(quotationMarkWordPositions.AreInitialAndFinalRatesSimilar("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        Assert.IsTrue(quotationMarkWordPositions.AreInitialAndFinalRatesSimilar("\u201d"));

        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        Assert.IsFalse(quotationMarkWordPositions.AreInitialAndFinalRatesSimilar("\u201d"));

        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        Assert.IsTrue(quotationMarkWordPositions.AreInitialAndFinalRatesSimilar("\u201d"));
    }

    [Test]
    public void IsMarkCommonlyMidWord()
    {
        var quotationMarkWordPositions = new QuotationMarkWordPositions();
        Assert.IsFalse(quotationMarkWordPositions.IsMarkCommonlyMidWord("'"));

        quotationMarkWordPositions.CountMidWordApostrophe("'");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkCommonlyMidWord("'"));

        quotationMarkWordPositions.CountWordInitialApostrophe("'");
        quotationMarkWordPositions.CountWordFinalApostrophe("'");
        quotationMarkWordPositions.CountWordInitialApostrophe("'");
        quotationMarkWordPositions.CountWordFinalApostrophe("'");
        Assert.IsFalse(quotationMarkWordPositions.IsMarkCommonlyMidWord("'"));

        quotationMarkWordPositions.CountMidWordApostrophe("'");
        Assert.IsTrue(quotationMarkWordPositions.IsMarkCommonlyMidWord("'"));
    }

    [Test]
    public void QuotationMarkWordPositionsReset()
    {
        var quotationMarkWordPositions = new QuotationMarkWordPositions();
        quotationMarkWordPositions.CountWordInitialApostrophe("\u201d");
        quotationMarkWordPositions.CountWordFinalApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");
        quotationMarkWordPositions.CountMidWordApostrophe("\u201d");

        Assert.IsTrue(quotationMarkWordPositions.IsMarkCommonlyMidWord("\u201d"));

        quotationMarkWordPositions.Reset();

        Assert.IsFalse(quotationMarkWordPositions.IsMarkCommonlyMidWord("\u201d"));

        // QuotationMarkSequence tests
    }

    [Test]
    public void IsMarkMuchMoreCommonEarlier()
    {
        var quotationMarkSequences = new QuotationMarkSequences();
        Assert.IsFalse(quotationMarkSequences.IsMarkMuchMoreCommonEarlier("\""));

        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        Assert.IsTrue(quotationMarkSequences.IsMarkMuchMoreCommonEarlier("\""));

        quotationMarkSequences.CountLaterQuotationMark("\"");
        Assert.IsFalse(quotationMarkSequences.IsMarkMuchMoreCommonEarlier("\""));

        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        Assert.IsTrue(quotationMarkSequences.IsMarkMuchMoreCommonEarlier("\""));

        quotationMarkSequences.CountLaterQuotationMark("\"");
        Assert.IsFalse(quotationMarkSequences.IsMarkMuchMoreCommonEarlier("\""));
    }

    [Test]
    public void IsMarkMuchMoreCommonLater()
    {
        var quotationMarkSequences = new QuotationMarkSequences();
        Assert.IsFalse(quotationMarkSequences.IsMarkMuchMoreCommonLater("\""));

        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        Assert.IsTrue(quotationMarkSequences.IsMarkMuchMoreCommonLater("\""));

        quotationMarkSequences.CountEarlierQuotationMark("\"");
        Assert.IsFalse(quotationMarkSequences.IsMarkMuchMoreCommonLater("\""));

        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        Assert.IsTrue(quotationMarkSequences.IsMarkMuchMoreCommonLater("\""));

        quotationMarkSequences.CountEarlierQuotationMark("\"");
        Assert.IsFalse(quotationMarkSequences.IsMarkMuchMoreCommonLater("\""));
    }

    [Test]
    public void IsMarkCommonEarlyAndLate()
    {
        var quotationMarkSequences = new QuotationMarkSequences();
        Assert.IsFalse(quotationMarkSequences.AreEarlyAndLateMarkRatesSimilar("\""));

        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        Assert.IsTrue(quotationMarkSequences.AreEarlyAndLateMarkRatesSimilar("\""));

        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountEarlierQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        Assert.IsTrue(quotationMarkSequences.AreEarlyAndLateMarkRatesSimilar("\""));

        quotationMarkSequences.CountLaterQuotationMark("\"");
        Assert.IsTrue(quotationMarkSequences.AreEarlyAndLateMarkRatesSimilar("\""));

        quotationMarkSequences.CountLaterQuotationMark("\"");
        quotationMarkSequences.CountLaterQuotationMark("\"");
        Assert.IsFalse(quotationMarkSequences.AreEarlyAndLateMarkRatesSimilar("\""));

        // QuotationMarkGrouper tests
    }

    [Test]
    public void GetQuotationMarkPairs()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var typewriterEnglishQuoteConvention = new QuoteConvention(
            "typewriter_english",
            [
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
            ]
        );

        var quotationMarkGrouper = new QuotationMarkGrouper(
            [],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([]));

        // no paired quotation mark
        quotationMarkGrouper = new QuotationMarkGrouper(
            [new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([]));

        // basic quotation mark pair
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 1, 2),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([("\u201c", "\u201d")]));

        // out-of-order quotation mark pair
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d\u201c").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d\u201c").Build(), 1, 2),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([]));

        // multiple unpaired quotation marks
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u2019").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u2019").Build(), 1, 2),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([]));

        // paired and unpaired quotation marks
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u2018\u201d").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u2018\u201d").Build(), 1, 2),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u2018\u201d").Build(), 2, 3),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([("\u201c", "\u201d")]));

        // ambiguous unpaired quotation mark
        quotationMarkGrouper = new QuotationMarkGrouper(
            [new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)],
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([]));

        // paired ambiguous quotation marks
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"\"").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"\"").Build(), 1, 2),
            ],
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([("\"", "\"")]));

        // multiple paired quotation marks (should be skipped because we don't know how to pair them)
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("\u201c\u201d\u201c\u201d").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("\u201c\u201d\u201c\u201d").Build(),
                    1,
                    2
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("\u201c\u201d\u201c\u201d").Build(),
                    2,
                    3
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("\u201c\u201d\u201c\u201d").Build(),
                    3,
                    4
                ),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([]));

        // multiple different paired quotation marks
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("\u201c\u201d\u2018\u2019").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("\u201c\u201d\u2018\u2019").Build(),
                    1,
                    2
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("\u201c\u201d\u2018\u2019").Build(),
                    2,
                    3
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("\u201c\u201d\u2018\u2019").Build(),
                    3,
                    4
                ),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(
            quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([("\u201c", "\u201d"), ("\u2018", "\u2019")])
        );

        // second-level paired quotation marks
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018\u2019").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018\u2019").Build(), 1, 2),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([("\u2018", "\u2019")]));

        // quotation marks that don't match the convention set
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 1, 2),
            ],
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        Assert.That(quotationMarkGrouper.GetQuotationMarkPairs().SequenceEqual([]));
    }

    [Test]
    public void HasDistinctPairedQuotationMarks()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var typewriterEnglishQuoteConvention = new QuoteConvention(
            "typewriter_english",
            [
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
            ]
        );

        var quotationMarkGrouper = new QuotationMarkGrouper(
            [],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.IsFalse(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u201c"));
        Assert.IsFalse(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u201d"));
        Assert.IsFalse(quotationMarkGrouper.HasDistinctPairedQuotationMark(""));

        // basic paired quotation marks
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 1, 2),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.IsTrue(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u201c"));
        Assert.IsTrue(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u201d"));

        // second-level paired quotation marks
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018\u2019").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018\u2019").Build(), 1, 2),
            ],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.IsTrue(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u2018"));
        Assert.IsTrue(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u2019"));

        // only one half of the pair observed
        quotationMarkGrouper = new QuotationMarkGrouper(
            [new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),],
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.IsFalse(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u201c"));
        Assert.IsTrue(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u201d"));

        // quotation marks that don't match the convention set
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 1, 2),
            ],
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        Assert.IsFalse(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u201c"));
        Assert.IsFalse(quotationMarkGrouper.HasDistinctPairedQuotationMark("\u201d"));

        // ambiguous quotation marks
        quotationMarkGrouper = new QuotationMarkGrouper(
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"\"").Build(), 0, 1),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"\"").Build(), 1, 2),
            ],
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        Assert.IsFalse(quotationMarkGrouper.HasDistinctPairedQuotationMark("\""));

        // PreliminaryApostropheAnalyzer tests
    }

    [Test]
    public void ThatTheMarkMustBeAnApostrophe()
    {
        var preliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        preliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("alternative mid\u2019word apostrophe").Build(),
                    15,
                    16
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("mid\u2018word quotation mark").Build(),
                    3,
                    4
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("mid\u201cword quotation mark").Build(),
                    3,
                    4
                ),
            ]
        );
        Assert.IsTrue(preliminaryApostropheAnalyzer.IsApostropheOnly("'"));
        Assert.IsTrue(preliminaryApostropheAnalyzer.IsApostropheOnly("\u2019"));
        Assert.IsFalse(preliminaryApostropheAnalyzer.IsApostropheOnly("\u2018"));
        Assert.IsFalse(preliminaryApostropheAnalyzer.IsApostropheOnly("\u201c"));
        Assert.IsFalse(preliminaryApostropheAnalyzer.IsApostropheOnly("\u201d"));
    }

    [Test]
    public void ThatARarelyInitialOrFinalMarkIsAnApostrophe()
    {
        var negativePreliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        negativePreliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
            ]
        );
        Assert.IsFalse(negativePreliminaryApostropheAnalyzer.IsApostropheOnly("'"));

        var positivePreliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        positivePreliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "The proportion must be kept below 0.02, because quotation marks should occur relatively infrequently"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "Apostrophes, on the other hand, can be much more common, especially in non-English languages where they "
                            + "can indicate a glottal stop"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText("Technically Unicode has a separate character for the glottal stop, but it is rarely used")
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
            ]
        );
        Assert.IsTrue(positivePreliminaryApostropheAnalyzer.IsApostropheOnly("'"));
    }

    [Test]
    public void ThatAMarkWithSimilarFinalAndInitialRatesIsAnApostrophe()
    {
        var negativePreliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        negativePreliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "We need a ton of text here to keep the proportion low, since we have 8 apostrophes in this test"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "The proportion must be kept below 0.02, because quotation marks should occur relatively infrequently"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "Apostrophes, on the other hand, can be much more common, especially in non-English languages where they "
                            + "can indicate a glottal stop"
                    )
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
            ]
        );
        Assert.IsFalse(negativePreliminaryApostropheAnalyzer.IsApostropheOnly("'"));

        var negativePreliminaryApostropheAnalyzer2 = new PreliminaryApostropheAnalyzer();
        negativePreliminaryApostropheAnalyzer2.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "We need a ton of text here to keep the proportion low, since we have 8 apostrophes in this test"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "The proportion must be kept below 0.02, because quotation marks should occur relatively infrequently"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "Apostrophes, on the other hand, can be much more common, especially in non-English languages where they "
                            + "can indicate a glottal stop"
                    )
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
            ]
        );
        Assert.IsFalse(negativePreliminaryApostropheAnalyzer2.IsApostropheOnly("'"));

        var positivePreliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        positivePreliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "We need a ton of text here to keep the proportion low, since we have 8 apostrophes in this test"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "The proportion must be kept below 0.02, because quotation marks should occur relatively infrequently"
                    )
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "Apostrophes, on the other hand, can be much more common, especially in non-English languages where they "
                            + "can indicate a glottal stop"
                    )
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
            ]
        );
        Assert.IsTrue(positivePreliminaryApostropheAnalyzer.IsApostropheOnly("'"));
    }

    [Test]
    public void ThatACommonlyMidWordMarkIsAnApostrophe()
    {
        var negativePreliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        negativePreliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
            ]
        );
        Assert.IsFalse(negativePreliminaryApostropheAnalyzer.IsApostropheOnly("'"));

        var positivePreliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        positivePreliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("mid'word apostrophe").Build(), 3, 4),
            ]
        );
        Assert.IsTrue(positivePreliminaryApostropheAnalyzer.IsApostropheOnly("'"));
    }

    [Test]
    public void ThatAFrequentlyOccurringCharacterIsAnApostrophe()
    {
        var negativePreliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        negativePreliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [
                new TextSegment.Builder()
                    .SetText("Long text segment to help keep the proportion of apostrophes low")
                    .Build(),
                new TextSegment.Builder()
                    .SetText(
                        "If a mark appears very frequently in the text, it is likely an apostrophe, instead of a quotation mark"
                    )
                    .Build(),
            ],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
            ]
        );
        Assert.IsFalse(negativePreliminaryApostropheAnalyzer.IsApostropheOnly("'"));

        var positivePreliminaryApostropheAnalyzer = new PreliminaryApostropheAnalyzer();
        positivePreliminaryApostropheAnalyzer.ProcessQuotationMarks(
            [new TextSegment.Builder().SetText("Very short text").Build(),],
            [
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("'word initial apostrophe").Build(),
                    0,
                    1
                ),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word' final apostrophe").Build(), 4, 5),
            ]
        );
        Assert.IsTrue(positivePreliminaryApostropheAnalyzer.IsApostropheOnly("'"));

        // PreliminaryQuotationMarkAnalyzer tests
    }

    [Test]
    public void ThatQuotationMarkSequenceIsUsedToDetermineOpeningAndClosingQuotes()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var typewriterEnglishQuoteConvention = new QuoteConvention(
            "typewriter_english",
            [
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
            ]
        );
        var standardFrenchQuoteConvention = new QuoteConvention(
            "standard_french",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
            ]
        );

        var westernEuropeanQuoteConvention = new QuoteConvention(
            "western_european",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var preliminaryQuotationAnalyzer = new PreliminaryQuotationMarkAnalyzer(
            new QuoteConventionSet(
                [
                    standardEnglishQuoteConvention,
                    typewriterEnglishQuoteConvention,
                    standardFrenchQuoteConvention,
                    westernEuropeanQuoteConvention,
                    standardSwedishQuoteConvention,
                ]
            )
        );

        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText("initial text \u201c quoted English text \u201d final text")
                                        .Build()
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([standardEnglishQuoteConvention]))
        );

        preliminaryQuotationAnalyzer.Reset();
        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText("initial text \u201d quoted Swedish text \u201d final text")
                                        .Build(),
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([standardSwedishQuoteConvention]))
        );

        preliminaryQuotationAnalyzer.Reset();
        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText(
                                            "initial text \u00ab quoted French/Western European text \u00bb final text"
                                        )
                                        .Build(),
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([standardFrenchQuoteConvention, westernEuropeanQuoteConvention]))
        );

        preliminaryQuotationAnalyzer.Reset();
        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText("initial text \" quoted typewriter English text \" final text")
                                        .Build(),
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([typewriterEnglishQuoteConvention]))
        );

        preliminaryQuotationAnalyzer.Reset();
        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText("initial text \u201c quoted English text \u201d final text")
                                        .Build(),
                                    new TextSegment.Builder()
                                        .SetText("second level \u2018 English quotes \u2019")
                                        .Build(),
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([standardEnglishQuoteConvention]))
        );

        preliminaryQuotationAnalyzer.Reset();
        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText("initial text \" quoted typewriter English text \" final text")
                                        .Build(),
                                    new TextSegment.Builder().SetText("second level 'typewriter quotes'").Build(),
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([typewriterEnglishQuoteConvention]))
        );

        preliminaryQuotationAnalyzer.Reset();
        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText("initial text \u201c quoted English text \u201d final text")
                                        .Build(),
                                    new TextSegment.Builder()
                                        .SetText("the quotes \u201d in this segment \u201c are backwards")
                                        .Build(),
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([]))
        );

        preliminaryQuotationAnalyzer.Reset();
        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText(
                                            "first-level quotes \u2018 must be observed \u2019 to retain a quote convention"
                                        )
                                        .Build(),
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([]))
        );
    }

    [Test]
    public void ThatApostrophesNotConsideredAsQuotationMarks()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var typewriterEnglishQuoteConvention = new QuoteConvention(
            "typewriter_english",
            [
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
            ]
        );

        var preliminaryQuotationAnalyzer = new PreliminaryQuotationMarkAnalyzer(
            new QuoteConventionSet([standardEnglishQuoteConvention, typewriterEnglishQuoteConvention,])
        );

        Assert.That(
            preliminaryQuotationAnalyzer.NarrowDownPossibleQuoteConventions(
                [
                    new Chapter(
                        [
                            new Verse(
                                [
                                    new TextSegment.Builder()
                                        .SetText("ini'tial 'text \u201c quo'ted English text' \u201d fi'nal text")
                                        .Build()
                                ]
                            )
                        ]
                    )
                ]
            ),
            Is.EqualTo(new QuoteConventionSet([standardEnglishQuoteConvention]))
        );
    }
}
