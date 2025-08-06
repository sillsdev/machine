using NUnit.Framework;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class QuotationMarkResolverTests
{
    [Test]
    public void Reset()
    {
        DepthBasedQuotationMarkResolver quotationMarkResolver = new DepthBasedQuotationMarkResolver(
            new QuoteConventionDetectionResolutionSettings(QuoteConventions.Standard)
        );

        Assert.That(quotationMarkResolver.QuotationMarkResolverState.Quotations.Count(), Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuoteContinuerState.QuoteContinuerMarks.Count(), Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuotationMarkResolverState.CurrentDepth, Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuoteContinuerState.CurrentDepth, Is.EqualTo(0));

        quotationMarkResolver.Reset();

        Assert.That(quotationMarkResolver.QuotationMarkResolverState.Quotations.Count(), Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuoteContinuerState.QuoteContinuerMarks.Count(), Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuotationMarkResolverState.CurrentDepth, Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuoteContinuerState.CurrentDepth, Is.EqualTo(0));

        List<QuotationMarkStringMatch> quotationMarkStringMatches =
        [
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("Opening “quote").Build(), 8, 9),
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("Another opening ‘quote").Build(), 16, 17),
            new QuotationMarkStringMatch(
                new TextSegment.Builder()
                    .SetText("“‘quote continuer")
                    .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                    .Build(),
                0,
                1
            )
        ];

        quotationMarkResolver.ResolveQuotationMarks(quotationMarkStringMatches).ToList();
        Assert.That(quotationMarkResolver.QuotationMarkResolverState.Quotations.Count(), Is.GreaterThan(0));
        Assert.IsTrue(quotationMarkResolver.QuotationMarkResolverState.CurrentDepth > 0);

        quotationMarkResolver.Reset();

        Assert.That(quotationMarkResolver.QuotationMarkResolverState.Quotations.Count(), Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuoteContinuerState.QuoteContinuerMarks.Count(), Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuotationMarkResolverState.CurrentDepth, Is.EqualTo(0));
        Assert.That(quotationMarkResolver.QuoteContinuerState.CurrentDepth, Is.EqualTo(0));
    }
}
