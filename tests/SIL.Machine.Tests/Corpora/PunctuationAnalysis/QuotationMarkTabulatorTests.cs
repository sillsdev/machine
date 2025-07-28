using NUnit.Framework;

namespace SIL.Machine.Corpora.PunctuationAnalysis;

[TestFixture]
public class QuotationMarkTabulatorTests
{
    [Test]
    public void GetObservedCount()
    {
        var counts = new QuotationMarkCounts();
        ;
        Assert.That(counts.TotalCount, Is.EqualTo(0));

        counts.CountQuotationMark("\"");
        Assert.That(counts.TotalCount, Is.EqualTo(1));

        counts.CountQuotationMark("\"");
        Assert.That(counts.TotalCount, Is.EqualTo(2));

        counts.CountQuotationMark("'");
        Assert.That(counts.TotalCount, Is.EqualTo(3));
    }

    [Test]
    public void GetBestProportion()
    {
        var counts = new QuotationMarkCounts();
        counts.CountQuotationMark("\"");
        counts.CountQuotationMark("\"");
        counts.CountQuotationMark("'");

        (string bestStr, int bestCount, int totalCount) = counts.FindBestQuotationMarkProportion();
        Assert.That(bestStr, Is.EqualTo("\""));
        Assert.That(bestCount, Is.EqualTo(2));
        Assert.That(totalCount, Is.EqualTo(3));

        counts.CountQuotationMark("'");
        counts.CountQuotationMark("'");

        (bestStr, bestCount, totalCount) = counts.FindBestQuotationMarkProportion();
        Assert.That(bestStr, Is.EqualTo("'"));
        Assert.That(bestCount, Is.EqualTo(3));
        Assert.That(totalCount, Is.EqualTo(5));
    }

    [Test]
    public void CalculateNumDifferences()
    {
        var counts = new QuotationMarkCounts();
        counts.CountQuotationMark("\"");
        counts.CountQuotationMark("\"");
        counts.CountQuotationMark("'");

        Assert.That(counts.CalculateNumDifferences("\""), Is.EqualTo(1));
        Assert.That(counts.CalculateNumDifferences("'"), Is.EqualTo(2));
        Assert.That(counts.CalculateNumDifferences("\u201c"), Is.EqualTo(3));

        counts.CountQuotationMark("'");
        Assert.That(counts.CalculateNumDifferences("\""), Is.EqualTo(2));
        Assert.That(counts.CalculateNumDifferences("'"), Is.EqualTo(2));
        Assert.That(counts.CalculateNumDifferences("\u201c"), Is.EqualTo(4));

        // QuotationMarkTabulator tests
    }

    [Test]
    public void CalculateSimilarity()
    {
        var singleLevelQuotationMarkTabulator = new QuotationMarkTabulator();
        singleLevelQuotationMarkTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u201d",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
            ]
        );

        Assert.That(
            singleLevelQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention("", [new SingleLevelQuoteConvention("\u201c", "\u201d")])
            ),
            Is.EqualTo(1.0)
        );
        ;
        Assert.That(
            singleLevelQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention("", [new SingleLevelQuoteConvention("\u201d", "\u201c")])
            ),
            Is.EqualTo(0.0)
        );
        ;
        Assert.That(
            singleLevelQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention("", [new SingleLevelQuoteConvention("\u201c", "\"")])
            ),
            Is.EqualTo(0.5)
        );
        ;
        Assert.That(
            singleLevelQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention(
                    "",
                    [
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb")
                    ]
                )
            ),
            Is.EqualTo(1.0)
        );

        var emptyQuotationMarkTabulator = new QuotationMarkTabulator();
        Assert.That(
            emptyQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention("", [new SingleLevelQuoteConvention("\u201c", "\u201d")])
            ),
            Is.EqualTo(0.0)
        );
        var twoLevelQuotationMarkTabulator = new QuotationMarkTabulator();
        twoLevelQuotationMarkTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u201d",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u2018",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    2
                ),
                new QuotationMarkMetadata(
                    "\u2019",
                    2,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    0,
                    2
                ),
            ]
        );
        Assert.That(
            twoLevelQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention("", [new SingleLevelQuoteConvention("\u201c", "\u201d")])
            ),
            Is.EqualTo(0.66666666666667).Within(1e-9)
        );
        Assert.That(
            twoLevelQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention(
                    "",
                    [
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019")
                    ]
                )
            ),
            Is.EqualTo(1.0)
        );
        Assert.That(
            twoLevelQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention(
                    "",
                    [
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb")
                    ]
                )
            ),
            Is.EqualTo(0.66666666666667).Within(1e-9)
        );
        Assert.That(
            twoLevelQuotationMarkTabulator.CalculateSimilarity(
                new QuoteConvention(
                    "",
                    [
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019")
                    ]
                )
            ),
            Is.EqualTo(0.33333333333333).Within(1e-9)
        );
        //
        //
    }
}
