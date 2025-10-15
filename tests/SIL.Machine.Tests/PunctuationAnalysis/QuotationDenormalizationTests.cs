using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class QuotationDenormalizationTests
{
    [Test]
    public void FullQuotationDenormalizationPipeline()
    {
        string normalizedUsfm =
            @"
    \id GEN
    \c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    \v 2 The woman said to the serpent,
    ""We may eat fruit from the trees of the garden,
    \v 3 but not the fruit of the tree which is in the middle of the garden.
    God has said, 'You shall not eat of it. You shall not touch it, lest you die.'""
    ";

        string expectedDenormalizedUsfm =
            @"\id GEN
\c 1
\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”
\v 2 The woman said to the serpent, “We may eat fruit from the trees of the garden,
\v 3 but not the fruit of the tree which is in the middle of the garden. God has said, ‘You shall not eat of it. You shall not touch it, lest you die.’”
";

        QuoteConvention standardEnglishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
            "standard_english"
        );
        Assert.IsNotNull(standardEnglishQuoteConvention);

        var quotationMarkDenormalizationFirstPass = new QuotationMarkDenormalizationFirstPass(
            standardEnglishQuoteConvention
        );

        UsfmParser.Parse(normalizedUsfm, quotationMarkDenormalizationFirstPass);
        List<QuotationMarkUpdateStrategy> bestChapterStrategies =
            quotationMarkDenormalizationFirstPass.FindBestChapterStrategies();

        var quotationMarkDenormalizer = new QuotationMarkDenormalizationUsfmUpdateBlockHandler(
            standardEnglishQuoteConvention,
            new QuotationMarkUpdateSettings(chapterStrategies: bestChapterStrategies)
        );

        var updater = new UpdateUsfmParserHandler(updateBlockHandlers: [quotationMarkDenormalizer]);
        UsfmParser.Parse(normalizedUsfm, updater);

        string actualDenormalizedUsfm = updater.GetUsfm();

        Assert.That(actualDenormalizedUsfm, Is.EqualTo(expectedDenormalizedUsfm).IgnoreLineEndings());
    }
}
