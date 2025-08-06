using NUnit.Framework;
using SIL.Machine.PunctuationAnalysis;

namespace SIL.Machine.Corpora;

[TestFixture]
public class QuotationMarkDenormalizationUsfmUpdateBlockHandlerTests
{
    private const string SimpleNormalizedUsfm =
        @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";

    [Test]
    public void SimpleEnglishQuoteDenormalization()
    {
        var normalizedUsfm = SimpleNormalizedUsfm;
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_english", "standard_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleBritishEnglishQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, 'Has God really said,
    ""You shall not eat of any tree of the garden""?'
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ‘Has God really said, “You shall not eat of any tree of the garden”?’"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "british_english", "british_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // no denormalization should be needed for this example
    }

    [Test]
    public void SimpleTypewriterEnglishQuoteDenormalization()
    {
        var normalizedUsfm = SimpleNormalizedUsfm;
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, \"Has God really said, 'You shall not eat of any tree of the garden'?\""
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_english", "typewriter_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // some of the quotes shouldn't need to be denormalized
    }

    [Test]
    public void SimpleHybridTypewriterEnglishQuoteDenormalization()
    {
        var normalizedUsfm = SimpleNormalizedUsfm;
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, 'You shall not eat of any tree of the garden'?”"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_english", "hybrid_typewriter_english");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // the single guillemets shouldn't need to be denormalized
        // because Moses doesn't normalize them
    }

    [Test]
    public void SimpleFrenchQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        ‹You shall not eat of any tree of the garden›?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, «Has God really said, ‹You shall not eat of any tree of the garden›?»"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_french", "standard_french");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // the unusual quotation marks shouldn't need to be denormalized
    }

    [Test]
    public void SimpleTypewriterFrenchQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, <<Has God really said,
    <You shall not eat of any tree of the garden>?>>
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, <<Has God really said, <You shall not eat of any tree of the garden>?>>"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "typewriter_french", "typewriter_french");
        AssertUsfmEqual(observedUsfm, expectedUsfm);

        // the 1st- and 2nd-level quotes are denormalized to identical marks
    }

    [Test]
    public void SimpleWesternEuropeanQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, «Has God really said, “You shall not eat of any tree of the garden”?»"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "western_european", "western_european");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleTypewriterWesternEuropeanQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, <<Has God really said,
    ""You shall not eat of any tree of the garden""?>>
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, <<Has God really said, \"You shall not eat of any tree of the garden\"?>>"
        );

        var observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "typewriter_western_european",
            "typewriter_western_european"
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleTypewriterWesternEuropeanVariantQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    <You shall not eat of any tree of the garden>?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, \"Has God really said, <You shall not eat of any tree of the garden>?\""
        );

        var observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "typewriter_western_european_variant",
            "typewriter_western_european_variant"
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleHybridTypewriterWesternEuropeanQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, «Has God really said, \"You shall not eat of any tree of the garden\"?»"
        );

        var observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "hybrid_typewriter_western_european",
            "hybrid_typewriter_western_european"
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleCentralEuropeanQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, „Has God really said, ‚You shall not eat of any tree of the garden‘?“"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "central_european", "central_european");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleCentralEuropeanGuillemetsQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        ›You shall not eat of any tree of the garden‹?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, »Has God really said, ›You shall not eat of any tree of the garden‹?«"
        );

        var observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "central_european_guillemets",
            "central_european_guillemets"
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleSwedishQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ”Has God really said, ’You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_swedish", "standard_swedish");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleFinnishQuoteDenormalization()
    {
        var normalizedUsfm = SimpleNormalizedUsfm;
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, »Has God really said, ’You shall not eat of any tree of the garden’?»"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_english", "standard_finnish");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleEasternEuropeanQuoteDenormalization()
    {
        var normalizedUsfm = SimpleNormalizedUsfm;
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, „Has God really said, ‚You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_english", "eastern_european");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleRussianQuoteDenormalization()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, «Has God really said, „You shall not eat of any tree of the garden“?»"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_russian", "standard_russian");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void SimpleArabicQuoteDenormalization()
    {
        var normalizedUsfm = SimpleNormalizedUsfm;
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ”Has God really said, ’You shall not eat of any tree of the garden‘?“"
        );

        var observedUsfm = DenormalizeQuotationMarks(normalizedUsfm, "standard_english", "standard_arabic");
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackQuotationDenormalizationSameAsFull()
    {
        var normalizedUsfm = SimpleNormalizedUsfm;
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, ‘You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            "standard_english",
            new QuotationMarkUpdateSettings(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackQuotationDenormalizationIncorrectlyNested()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    ""You shall not eat of any tree of the garden""?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, “You shall not eat of any tree of the garden”?”"
        );

        var observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            "standard_english",
            new QuotationMarkUpdateSettings(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackQuotationDenormalizationIncorrectlyNestedSecondCase()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, 'Has God really said,
    ""You shall not eat of any tree of the garden""?'
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, ‘Has God really said, “You shall not eat of any tree of the garden”?’"
        );

        var observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            "standard_english",
            new QuotationMarkUpdateSettings(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    [Test]
    public void FallbackQuotationDenormalizationUnclosedQuote()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
        You shall not eat of any tree of the garden'?""
    ";
        var expectedUsfm = (
            "\\c 1\n"
            + "\\v 1 Now the serpent was more subtle than any animal of the field which Yahweh God had made. He said to "
            + "the woman, “Has God really said, You shall not eat of any tree of the garden’?”"
        );

        var observedUsfm = DenormalizeQuotationMarks(
            normalizedUsfm,
            "standard_english",
            "standard_english",
            new QuotationMarkUpdateSettings(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        AssertUsfmEqual(observedUsfm, expectedUsfm);
    }

    public string DenormalizeQuotationMarks(
        string normalizedUsfm,
        string sourceQuoteConventionName,
        string targetQuoteConventionName,
        QuotationMarkUpdateSettings? quotationDenormalizationSettings = null
    )
    {
        quotationDenormalizationSettings ??= new QuotationMarkUpdateSettings();
        QuotationMarkDenormalizationUsfmUpdateBlockHandler quotationDenormalizer = (
            CreateQuotationDenormalizationUsfmUpdateBlockHandler(
                sourceQuoteConventionName,
                targetQuoteConventionName,
                quotationDenormalizationSettings
            )
        );

        var updater = new UpdateUsfmParserHandler(updateBlockHandlers: [quotationDenormalizer]);
        UsfmParser.Parse(normalizedUsfm, updater);

        return updater.GetUsfm();
    }

    public QuotationMarkDenormalizationUsfmUpdateBlockHandler CreateQuotationDenormalizationUsfmUpdateBlockHandler(
        string sourceQuoteConventionName,
        string targetQuoteConventionName,
        QuotationMarkUpdateSettings? quotationDenormalizationSettings = null
    )
    {
        quotationDenormalizationSettings ??= new QuotationMarkUpdateSettings();
        var sourceQuoteConvention = GetQuoteConventionByName(sourceQuoteConventionName);
        var targetQuoteConvention = GetQuoteConventionByName(targetQuoteConventionName);

        return new QuotationMarkDenormalizationUsfmUpdateBlockHandler(
            sourceQuoteConvention,
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
