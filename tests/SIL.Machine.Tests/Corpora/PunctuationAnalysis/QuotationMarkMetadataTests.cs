using NUnit.Framework;

namespace SIL.Machine.Corpora.PunctuationAnalysis;

[TestFixture]
public class QuotationMarkMetadataTests
{
    [Test]
    public void UpdateQuotationMark()
    {
        var quotationMarkMetadata = new QuotationMarkMetadata(
            quotationMark: "\"",
            depth: 1,
            direction: QuotationMarkDirection.Opening,
            textSegment: new TextSegment.Builder().SetText("He said to the woman, \"Has God really said,").Build(),
            startIndex: 22,
            endIndex: 23
        );
        quotationMarkMetadata.UpdateQuotationMark(GetQuoteConventionByName("standard_english"));
        Assert.That(quotationMarkMetadata.TextSegment.Text, Is.EqualTo("He said to the woman, “Has God really said,"));

        quotationMarkMetadata = new QuotationMarkMetadata(
            quotationMark: "\"",
            depth: 1,
            direction: QuotationMarkDirection.Opening,
            textSegment: new TextSegment.Builder().SetText("He said to the woman, \"Has God really said,").Build(),
            startIndex: 22,
            endIndex: 23
        );
        quotationMarkMetadata.UpdateQuotationMark(GetQuoteConventionByName("western_european"));
        Assert.That(quotationMarkMetadata.TextSegment.Text, Is.EqualTo("He said to the woman, «Has God really said,"));

        quotationMarkMetadata = new QuotationMarkMetadata(
            quotationMark: "\"",
            depth: 1,
            direction: QuotationMarkDirection.Opening,
            textSegment: new TextSegment.Builder().SetText("He said to the woman, \"Has God really said,").Build(),
            startIndex: 23,
            endIndex: 24
        );
        quotationMarkMetadata.UpdateQuotationMark(GetQuoteConventionByName("western_european"));
        Assert.That(quotationMarkMetadata.TextSegment.Text, Is.EqualTo("He said to the woman, \"«as God really said,"));
    }

    [Test]
    public void UpdateQuotationMarkWithMultiCharacterQuotationMarks()
    {
        var quotationMarkMetadata = new QuotationMarkMetadata(
            quotationMark: "\"",
            depth: 1,
            direction: QuotationMarkDirection.Opening,
            textSegment: new TextSegment.Builder().SetText("He said to the woman, \"Has God really said,").Build(),
            startIndex: 22,
            endIndex: 23
        );
        quotationMarkMetadata.UpdateQuotationMark(GetQuoteConventionByName("typewriter_french"));
        Assert.That(quotationMarkMetadata.TextSegment.Text, Is.EqualTo("He said to the woman, <<Has God really said,"));
        Assert.That(quotationMarkMetadata.StartIndex, Is.EqualTo(22));
        Assert.That(quotationMarkMetadata.EndIndex, Is.EqualTo(24));

        quotationMarkMetadata = new QuotationMarkMetadata(
            quotationMark: "<<",
            depth: 1,
            direction: QuotationMarkDirection.Opening,
            textSegment: new TextSegment.Builder().SetText("He said to the woman, <<Has God really said,").Build(),
            startIndex: 22,
            endIndex: 24
        );
        quotationMarkMetadata.UpdateQuotationMark(GetQuoteConventionByName("standard_english"));
        Assert.That(quotationMarkMetadata.TextSegment.Text, Is.EqualTo("He said to the woman, “Has God really said,"));
        Assert.That(quotationMarkMetadata.StartIndex, Is.EqualTo(22));
        Assert.That(quotationMarkMetadata.EndIndex, Is.EqualTo(23));
    }

    public QuoteConvention GetQuoteConventionByName(string name)
    {
        QuoteConvention quoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(name);
        Assert.IsNotNull(quoteConvention);
        return quoteConvention;
    }
}
