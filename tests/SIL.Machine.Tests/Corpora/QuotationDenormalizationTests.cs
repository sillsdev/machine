using NUnit.Framework;
using SIL.Machine.Corpora.PunctuationAnalysis;

namespace SIL.Machine.Corpora;

[TestFixture]
public class QuotationDenormalizationTests
{
    [Test]
    public void FullQuotationDenormalizationPipeline()
    {
        var normalizedUsfm =
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

        var expectedDenormalizedUsfm =
            @"\id GEN
\c 1
\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”
\v 2 The woman said to the serpent, “We may eat fruit from the trees of the garden,
\v 3 but not the fruit of the tree which is in the middle of the garden. God has said, ‘You shall not eat of it. You shall not touch it, lest you die.’”
";

        var standardEnglishQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            "standard_english"
        );
        Assert.IsNotNull(standardEnglishQuoteConvention);

        var quotationMarkDenormalizationFirstPass = new QuotationMarkDenormalizationFirstPass(
            standardEnglishQuoteConvention,
            standardEnglishQuoteConvention
        );

        UsfmParser.Parse(normalizedUsfm, quotationMarkDenormalizationFirstPass);
        var bestChapterStrategies = quotationMarkDenormalizationFirstPass.FindBestChapterStrategies();

        var quotationMarkDenormalizer = new QuotationMarkDenormalizationUsfmUpdateBlockHandler(
            standardEnglishQuoteConvention,
            standardEnglishQuoteConvention,
            new QuotationMarkUpdateSettings(chapterActions: bestChapterStrategies)
        );

        var updater = new UpdateUsfmParserHandler(updateBlockHandlers: [quotationMarkDenormalizer]);
        UsfmParser.Parse(normalizedUsfm, updater);

        var actualDenormalizedUsfm = updater.GetUsfm();

        Assert.That(actualDenormalizedUsfm, Is.EqualTo(expectedDenormalizedUsfm).IgnoreLineEndings()); //TODO use ignore_line_endings
    }
}
