using NUnit.Framework;

namespace SIL.Machine.Corpora.PunctuationAnalysis;

[TestFixture]
public class QuoteConventionDetectorTests
{
    // Text comes from the World English Bible, which is in the public domain.
    [Test]
    public void StandardEnglish()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?”
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_english"));
    }

    [Test]
    public void TypewriterEnglish()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    'You shall not eat of any tree of the garden'?\""
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("typewriter_english"));
    }

    [Test]
    public void BritishEnglish()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ‘Has God really said,
    “You shall not eat of any tree of the garden”?’
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("british_english"));
    }

    [Test]
    public void BritishTypewriterEnglish()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, 'Has God really said,
    ""You shall not eat of any tree of the garden""?'
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("british_typewriter_english"));
    }

    [Test]
    public void HybridTypewriterEnglish()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    'You shall not eat of any tree of the garden'?”
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("hybrid_typewriter_english"));
    }

    [Test]
    public void StandardFrench()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    ‹You shall not eat of any tree of the garden›?»
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_french"));
    }

    [Test]
    public void TypewriterFrench()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, <<Has God really said,
    <You shall not eat of any tree of the garden>?>>
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("typewriter_french"));
    }

    // frenchVariant requires a 3rd-level of quotes to differentiate from standardFrench
    [Test]
    public void WesternEuropean()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    “You shall not eat of any tree of the garden”?»
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("western_european"));
    }

    [Test]
    public void BritishInspiredWesternEuropean()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    ‘You shall not eat of any tree of the garden’?»
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("british_inspired_western_european"));
    }

    [Test]
    public void TypewriterWesternEuropean()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, <<Has God really said,
    ""You shall not eat of any tree of the garden""?>>
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("typewriter_western_european"));
    }

    [Test]
    public void TypewriterWesternEuropeanVariant()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ""Has God really said,
    <You shall not eat of any tree of the garden>?""
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("typewriter_western_european_variant"));
    }

    [Test]
    public void HybridTypewriterWesternEuropean()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    ""You shall not eat of any tree of the garden""?»
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("hybrid_typewriter_western_european"));
    }

    [Test]
    public void HybridBritishTypewriterWesternEuropean()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    'You shall not eat of any tree of the garden'?»
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("hybrid_british_typewriter_western_european"));
    }

    [Test]
    public void CentralEuropean()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, „Has God really said,
    ‚You shall not eat of any tree of the garden‘?“
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("central_european"));
    }

    [Test]
    public void CentralEuropeanGuillemets()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, »Has God really said,
    ›You shall not eat of any tree of the garden‹?«
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("central_european_guillemets"));
    }

    [Test]
    public void StandardSwedish()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ”Has God really said,
    ’You shall not eat of any tree of the garden’?”
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_swedish"));
    }

    [Test]
    public void StandardFinnish()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, »Has God really said,
    ’You shall not eat of any tree of the garden’?»
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_finnish"));
    }

    [Test]
    public void EasternEuropean()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, „Has God really said,
    ‚You shall not eat of any tree of the garden’?”
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("eastern_european"));
    }

    [Test]
    public void StandardRussian()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    „You shall not eat of any tree of the garden“?»
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_russian"));
    }

    [Test]
    public void StandardArabic()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, ”Has God really said,
    ’You shall not eat of any tree of the garden‘?“
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_arabic"));
    }

    [Test]
    public void NonStandardArabic()
    {
        var usfm =
            @"
\c 1
\v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, «Has God really said,
    ’You shall not eat of any tree of the garden‘?»
    ";
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("non-standard_arabic"));
    }

    [Test]
    public void MismatchedQuotationMarks()
    {
        var usfm =
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
        var analysis = DetectQuotationConvention(usfm);
        Assert.IsNotNull(analysis);
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_english"));
    }

    public QuoteConventionAnalysis DetectQuotationConvention(string usfm)
    {
        var quoteConventionDetector = new QuoteConventionDetector();
        UsfmParser.Parse(usfm, quoteConventionDetector);
        return quoteConventionDetector.DetectQuotationConvention();
    }
}
