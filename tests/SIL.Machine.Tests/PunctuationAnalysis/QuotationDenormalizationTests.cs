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
    \c 2
    \v 1 Thus the heavens and the earth were completed in all their vast array.
    \v 2 And by the seventh day God had finished the work He had been doing;
    so on that day He rested from all His work.
    \v 3 Then God blessed the seventh day and sanctified it,
    because on that day He rested from all the work of creation that He had accomplished.
    \c 3
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
\c 2
\v 1 Thus the heavens and the earth were completed in all their vast array.
\v 2 And by the seventh day God had finished the work He had been doing; so on that day He rested from all His work.
\v 3 Then God blessed the seventh day and sanctified it, because on that day He rested from all the work of creation that He had accomplished.
\c 3
\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”
\v 2 The woman said to the serpent, “We may eat fruit from the trees of the garden,
\v 3 but not the fruit of the tree which is in the middle of the garden. God has said, ‘You shall not eat of it. You shall not touch it, lest you die.’”
";

        QuoteConvention standardEnglishQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
            "standard_english"
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);

        var quotationMarkDenormalizationFirstPass = new QuotationMarkDenormalizationFirstPass(
            standardEnglishQuoteConvention
        );

        UsfmParser.Parse(normalizedUsfm, quotationMarkDenormalizationFirstPass);
        List<(int ChapterNumber, QuotationMarkUpdateStrategy Strategy)> bestChapterStrategies =
            quotationMarkDenormalizationFirstPass.FindBestChapterStrategies();

        Assert.That(bestChapterStrategies.Select(tuple => tuple.ChapterNumber).SequenceEqual([2, 3]));

        var quotationMarkDenormalizer = new QuotationMarkDenormalizationUsfmUpdateBlockHandler(
            standardEnglishQuoteConvention,
            new QuotationMarkUpdateSettings(
                chapterStrategies: bestChapterStrategies.Select(tuple => tuple.Strategy).ToList()
            )
        );

        var updater = new UpdateUsfmParserHandler(updateBlockHandlers: [quotationMarkDenormalizer]);
        UsfmParser.Parse(normalizedUsfm, updater);

        string actualDenormalizedUsfm = updater.GetUsfm();

        Assert.That(actualDenormalizedUsfm, Is.EqualTo(expectedDenormalizedUsfm).IgnoreLineEndings());
    }
}
