using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class QuotationMarkDenormalizationUsfmUpdateBlockHandlerTests
{
    [Test]
    public void SimpleEnglishQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        ;
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleBritishEnglishQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, 'Has God really said,
    ""You shall not eat of any tree of the garden""?'
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ‘Has God really said, “You shall not eat of any tree of the garden”?’"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "british_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    // no denormalization should be needed for this example
    [Test]
    public void SimpleTypewriterEnglishQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        ;
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, \"Has God really said, 'You shall not eat of any tree of the garden'?\""
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "typewriter_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    // some of the quotes shouldn't need to be denormalized
    [Test]
    public void SimpleHybridTypewriterEnglishQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        ;
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, 'You shall not eat of any tree of the garden'?”"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "hybrid_typewriter_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // the single guillemets shouldn't need to be denormalized
        // because Moses doesn't normalize them
    }

    [Test]
    public void SimpleFrenchQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        ‹You shall not eat of any tree of the garden›?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, «Has God really said, ‹You shall not eat of any tree of the garden›?»"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_french");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // the unusual quotation marks shouldn't need to be denormalized
    }

    [Test]
    public void SimpleTypewriterFrenchQuoteDenormalization()
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
            + "the woman, <<Has God really said, <You shall not eat of any tree of the garden>?>>"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "typewriter_french");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    // the 1st- and 2nd-level quotes are denormalized to identical marks
    [Test]
    public void SimpleWesternEuropeanQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, «Has God really said, “You shall not eat of any tree of the garden”?»"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "western_european");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleTypewriterWesternEuropeanQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, <<Has God really said,
    ""You shall not eat of any tree of the garden""?>>
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, <<Has God really said, \"You shall not eat of any tree of the garden\"?>>"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "typewriter_western_european");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleTypewriterWesternEuropeanVariantQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    <You shall not eat of any tree of the garden>?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, \"Has God really said, <You shall not eat of any tree of the garden>?\""
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "typewriter_western_european_variant");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleHybridTypewriterWesternEuropeanQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, «Has God really said, \"You shall not eat of any tree of the garden\"?»"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "hybrid_typewriter_western_european");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleCentralEuropeanQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, „Has God really said, ‚You shall not eat of any tree of the garden‘?“"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "central_european");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleCentralEuropeanGuillemetsQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        ›You shall not eat of any tree of the garden‹?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, »Has God really said, ›You shall not eat of any tree of the garden‹?«"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "central_european_guillemets");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleSwedishQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ”Has God really said, ’You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_swedish");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleFinnishQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        ;
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, »Has God really said, ’You shall not eat of any tree of the garden’?»"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_finnish");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleEasternEuropeanQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        ;
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, „Has God really said, ‚You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "eastern_european");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleRussianQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, «Has God really said, „You shall not eat of any tree of the garden“?»"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_russian");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleArabicQuoteDenormalization()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        ;
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ”Has God really said, ’You shall not eat of any tree of the garden‘?“"
        );

        string observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_arabic");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackQuotationDenormalizationSameAsFull()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        ;
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            new QuotationMarkUpdateSettings(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackQuotationDenormalizationIncorrectlyNested()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, “You shall not eat of any tree of the garden”?”"
        );

        string observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            new QuotationMarkUpdateSettings(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackQuotationDenormalizationIncorrectlyNestedSecondCase()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, 'Has God really said,
    ""You shall not eat of any tree of the garden""?'
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ‘Has God really said, “You shall not eat of any tree of the garden”?’"
        );

        string observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            new QuotationMarkUpdateSettings(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackQuotationDenormalizationUnclosedQuote()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        You shall not eat of any tree of the garden'?""
    ";
        string expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        string observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            new QuotationMarkUpdateSettings(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    public string DenormalizeQuotationMarks(
        string normalizedUsfm,
        string targetQuoteConventionName,
        QuotationMarkUpdateSettings? quotationDenormalizationSettings = null
    )
    {
        quotationDenormalizationSettings ??= new QuotationMarkUpdateSettings();
        QuotationMarkDenormalizationUsfmUpdateBlockHandler quotationDenormalizer = (
            CreateQuotationDenormalizationUsfmUpdateBlockHandler(
                targetQuoteConventionName,
                quotationDenormalizationSettings
            )
        );

        var updater = new UpdateUsfmParserHandler(updateBlockHandlers: [quotationDenormalizer]);
        UsfmParser.Parse(normalizedUsfm, updater);

        return updater.GetUsfm();
    }

    public QuotationMarkDenormalizationUsfmUpdateBlockHandler CreateQuotationDenormalizationUsfmUpdateBlockHandler(
        string targetQuoteConventionName,
        QuotationMarkUpdateSettings? quotationDenormalizationSettings = null
    )
    {
        quotationDenormalizationSettings ??= new QuotationMarkUpdateSettings();
        QuoteConvention targetQuoteConvention = GetQuoteConventionByName(targetQuoteConventionName);

        return new QuotationMarkDenormalizationUsfmUpdateBlockHandler(
            targetQuoteConvention,
            quotationDenormalizationSettings
        );
    }

    public void AssertUsfmEqual(string observedUsfm, string expectedUsfm)
    {
        foreach ((string observedLine, string expectedLine) in observedUsfm.Split("\n").Zip(expectedUsfm.Split("\n")))
        {
            Assert.That(observedLine.Trim(), Is.EqualTo(expectedLine.Trim()));
        }
    }

    public QuoteConvention GetQuoteConventionByName(string name)
    {
        QuoteConvention quoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(name);
        Assert.IsNotNull(quoteConvention);
        return quoteConvention;
    }
}
