using NUnit.Framework;
using SIL.Machine.Corpora;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class QuotationConventionDetectorTests
{
    // Text comes from the World English Bible, which is in the public domain.
    [Test]
    public void StandardEnglish()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?”
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_english"));
    }

    [Test]
    public void TypewriterEnglish()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?\""
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("typewriter_english"));
    }

    [Test]
    public void BritishEnglish()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ‘Has God really said,
    “You shall not eat of any tree of the garden”?’
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("british_english"));
    }

    [Test]
    public void BritishTypewriterEnglish()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, 'Has God really said,
    ""You shall not eat of any tree of the garden""?'
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("british_typewriter_english"));
    }

    [Test]
    public void HybridTypewriterEnglish()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    'You shall not eat of any tree of the garden'?”
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("hybrid_typewriter_english"));
    }

    [Test]
    public void StandardFrench()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    ‹You shall not eat of any tree of the garden›?»
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_french"));
    }

    [Test]
    public void TypewriterFrench()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, <<Has God really said,
    <You shall not eat of any tree of the garden>?>>
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("typewriter_french"));
    }

    // frenchVariant requires a 3rd-level of quotes to differentiate from standardFrench
    [Test]
    public void WesternEuropean()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    “You shall not eat of any tree of the garden”?»
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("western_european"));
    }

    [Test]
    public void BritishInspiredWesternEuropean()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    ‘You shall not eat of any tree of the garden’?»
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("british_inspired_western_european"));
    }

    [Test]
    public void TypewriterWesternEuropean()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, <<Has God really said,
    ""You shall not eat of any tree of the garden""?>>
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("typewriter_western_european"));
    }

    [Test]
    public void TypewriterWesternEuropeanVariant()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    <You shall not eat of any tree of the garden>?""
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("typewriter_western_european_variant"));
    }

    [Test]
    public void HybridTypewriterWesternEuropean()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    ""You shall not eat of any tree of the garden""?»
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("hybrid_typewriter_western_european"));
    }

    [Test]
    public void HybridBritishTypewriterWesternEuropean()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    'You shall not eat of any tree of the garden'?»
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("hybrid_british_typewriter_western_european"));
    }

    [Test]
    public void CentralEuropean()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, „Has God really said,
    ‚You shall not eat of any tree of the garden‘?“
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("central_european"));
    }

    [Test]
    public void CentralEuropeanGuillemets()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, »Has God really said,
    ›You shall not eat of any tree of the garden‹?«
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("central_european_guillemets"));
    }

    [Test]
    public void StandardSwedish()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ”Has God really said,
    ’You shall not eat of any tree of the garden’?”
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_swedish"));
    }

    [Test]
    public void StandardFinnish()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, »Has God really said,
    ’You shall not eat of any tree of the garden’?»
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_finnish"));
    }

    [Test]
    public void EasternEuropean()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, „Has God really said,
    ‚You shall not eat of any tree of the garden’?”
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("eastern_european"));
    }

    [Test]
    public void StandardRussian()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    „You shall not eat of any tree of the garden“?»
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_russian"));
    }

    [Test]
    public void StandardArabic()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ”Has God really said,
    ’You shall not eat of any tree of the garden‘?“
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_arabic"));
    }

    [Test]
    public void NonStandardArabic()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    ’You shall not eat of any tree of the garden‘?»
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("non-standard_arabic"));
    }

    [Test]
    public void MismatchedQuotationMarks()
    {
        string usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?”
    \\v 2 The woman said to the serpent,
    “We may eat fruit from the trees of the garden,
    \\v 3 but not the fruit of the tree which is in the middle of the garden.
    God has said, ‘You shall not eat of it. You shall not touch it, lest you die.’
    ";
        QuoteConventionAnalysis analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_english"));
    }

    public QuoteConventionAnalysis DetectQuotationConvention(string usfm)
    {
        var quoteConventionDetector = new QuoteConventionDetector();
        UsfmParser.Parse(usfm, quoteConventionDetector);
        return quoteConventionDetector.DetectQuoteConvention();
    }
}
