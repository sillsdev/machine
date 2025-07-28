using NUnit.Framework;

namespace SIL.Machine.Corpora.PunctuationAnalysis;

[TestFixture]
public class QuoteConventionTests
{
    [Test]
    public void SingleLevelQuoteConventionNormalize()
    {
        var englishLevel1QuoteConvention = new SingleLevelQuoteConvention("\u201c", "\u201d");
        var normalizedEnglishLevel1QuoteConvention = englishLevel1QuoteConvention.Normalize();
        Assert.That(normalizedEnglishLevel1QuoteConvention.OpeningQuotationMark, Is.EqualTo("\""));
        Assert.That(normalizedEnglishLevel1QuoteConvention.ClosingQuotationMark, Is.EqualTo("\""));

        var englishLevel2QuoteConvention = new SingleLevelQuoteConvention("\u2018", "\u2019");
        var normalizedEnglishLevel2QuoteConvention = englishLevel2QuoteConvention.Normalize();
        Assert.That(normalizedEnglishLevel2QuoteConvention.OpeningQuotationMark, Is.EqualTo("'"));
        Assert.That(normalizedEnglishLevel2QuoteConvention.ClosingQuotationMark, Is.EqualTo("'"));

        var alreadyNormalizedEnglishLevel1QuoteConvention = new SingleLevelQuoteConvention("\"", "\"");
        var doublyNormalizedEnglishLevel1QuoteConvention = alreadyNormalizedEnglishLevel1QuoteConvention.Normalize();
        Assert.That(doublyNormalizedEnglishLevel1QuoteConvention.OpeningQuotationMark, Is.EqualTo("\""));
        Assert.That(doublyNormalizedEnglishLevel1QuoteConvention.ClosingQuotationMark, Is.EqualTo("\""));

        var alreadyNormalizedEnglishLevel2QuoteConvention = new SingleLevelQuoteConvention("'", "'");
        var doublyNormalizedEnglishLevel2QuoteConvention = alreadyNormalizedEnglishLevel2QuoteConvention.Normalize();
        Assert.That(doublyNormalizedEnglishLevel2QuoteConvention.OpeningQuotationMark, Is.EqualTo("'"));
        Assert.That(doublyNormalizedEnglishLevel2QuoteConvention.ClosingQuotationMark, Is.EqualTo("'"));

        var frenchLevel1QuoteConvention = new SingleLevelQuoteConvention("\u00ab", "\u00bb");
        var normalizedFrenchLevel1QuoteConvention = frenchLevel1QuoteConvention.Normalize();
        Assert.That(normalizedFrenchLevel1QuoteConvention.OpeningQuotationMark, Is.EqualTo("\""));
        Assert.That(normalizedFrenchLevel1QuoteConvention.ClosingQuotationMark, Is.EqualTo("\""));

        var frenchLevel2QuoteConvention = new SingleLevelQuoteConvention("\u2039", "\u203a");
        var normalizedFrenchLevel2QuoteConvention = frenchLevel2QuoteConvention.Normalize();
        Assert.That(normalizedFrenchLevel2QuoteConvention.OpeningQuotationMark, Is.EqualTo("\u2039"));
        Assert.That(normalizedFrenchLevel2QuoteConvention.ClosingQuotationMark, Is.EqualTo("\u203a"));

        var typewriterFrenchLevel1QuoteConvention = new SingleLevelQuoteConvention("<<", ">>");
        var normalizedTypewriterFrenchLevel1QuoteConvention = typewriterFrenchLevel1QuoteConvention.Normalize();
        Assert.That(normalizedTypewriterFrenchLevel1QuoteConvention.OpeningQuotationMark, Is.EqualTo("<<"));
        Assert.That(normalizedTypewriterFrenchLevel1QuoteConvention.ClosingQuotationMark, Is.EqualTo(">>"));

        var typewriterFrenchLevel2QuoteConvention = new SingleLevelQuoteConvention("<", ">");
        var normalizedTypewriterFrenchLevel2QuoteConvention = typewriterFrenchLevel2QuoteConvention.Normalize();
        Assert.That(normalizedTypewriterFrenchLevel2QuoteConvention.OpeningQuotationMark, Is.EqualTo("<"));
        Assert.That(normalizedTypewriterFrenchLevel2QuoteConvention.ClosingQuotationMark, Is.EqualTo(">"));

        var centralEuropeanLevel1QuoteConvention = new SingleLevelQuoteConvention("\u201e", "\u201c");
        var normalizedCentralEuropeanLevel1QuoteConvention = centralEuropeanLevel1QuoteConvention.Normalize();
        Assert.That(normalizedCentralEuropeanLevel1QuoteConvention.OpeningQuotationMark, Is.EqualTo("\""));
        Assert.That(normalizedCentralEuropeanLevel1QuoteConvention.ClosingQuotationMark, Is.EqualTo("\""));

        var centralEuropeanLevel2QuoteConvention = new SingleLevelQuoteConvention("\u201a", "\u2018");
        var normalizedCentralEuropeanLevel2QuoteConvention = centralEuropeanLevel2QuoteConvention.Normalize();
        Assert.That(normalizedCentralEuropeanLevel2QuoteConvention.OpeningQuotationMark, Is.EqualTo("'"));
        Assert.That(normalizedCentralEuropeanLevel2QuoteConvention.ClosingQuotationMark, Is.EqualTo("'"));

        var centralEuropeanGuillemetsQuoteConvention = new SingleLevelQuoteConvention("\u00bb", "\u00ab");
        var normalizedCentralEuropeanGuillemetsQuoteConvention = centralEuropeanGuillemetsQuoteConvention.Normalize();
        Assert.That(normalizedCentralEuropeanGuillemetsQuoteConvention.OpeningQuotationMark, Is.EqualTo("\""));
        Assert.That(normalizedCentralEuropeanGuillemetsQuoteConvention.ClosingQuotationMark, Is.EqualTo("\""));

        var swedishLevel1QuoteConvention = new SingleLevelQuoteConvention("\u201d", "\u201d");
        var normalizedSwedishLevel1QuoteConvention = swedishLevel1QuoteConvention.Normalize();
        Assert.That(normalizedSwedishLevel1QuoteConvention.OpeningQuotationMark, Is.EqualTo("\""));
        Assert.That(normalizedSwedishLevel1QuoteConvention.ClosingQuotationMark, Is.EqualTo("\""));

        var swedishLevel2QuoteConvention = new SingleLevelQuoteConvention("\u2019", "\u2019");
        var normalizedSwedishLevel2QuoteConvention = swedishLevel2QuoteConvention.Normalize();
        Assert.That(normalizedSwedishLevel2QuoteConvention.OpeningQuotationMark, Is.EqualTo("'"));
        Assert.That(normalizedSwedishLevel2QuoteConvention.ClosingQuotationMark, Is.EqualTo("'"));

        var finnishLevel1QuoteConvention = new SingleLevelQuoteConvention("\u00bb", "\u00bb");
        var normalizedFinnishLevel1QuoteConvention = finnishLevel1QuoteConvention.Normalize();
        Assert.That(normalizedFinnishLevel1QuoteConvention.OpeningQuotationMark, Is.EqualTo("\""));
        Assert.That(normalizedFinnishLevel1QuoteConvention.ClosingQuotationMark, Is.EqualTo("\""));

        var arabicLevel1QuoteConvention = new SingleLevelQuoteConvention("\u201d", "\u201c");
        var normalizedArabicLevel1QuoteConvention = arabicLevel1QuoteConvention.Normalize();
        Assert.That(normalizedArabicLevel1QuoteConvention.OpeningQuotationMark, Is.EqualTo("\""));
        Assert.That(normalizedArabicLevel1QuoteConvention.ClosingQuotationMark, Is.EqualTo("\""));

        var arabicLevel2QuoteConvention = new SingleLevelQuoteConvention("\u2019", "\u2018");
        var normalizedArabicLevel2QuoteConvention = arabicLevel2QuoteConvention.Normalize();
        Assert.That(normalizedArabicLevel2QuoteConvention.OpeningQuotationMark, Is.EqualTo("'"));
        Assert.That(normalizedArabicLevel2QuoteConvention.ClosingQuotationMark, Is.EqualTo("'"));
    }

    [Test]
    public void GetNumLevels()
    {
        var emptyQuoteConvention = new QuoteConvention("empty-quote-convention", []);
        Assert.That(emptyQuoteConvention.NumLevels, Is.EqualTo(0));

        var oneLevelQuoteConvention = new QuoteConvention(
            "one-level-quote-convention",
            [new SingleLevelQuoteConvention("\u201c", "\u201d")]
        );
        Assert.That(oneLevelQuoteConvention.NumLevels, Is.EqualTo(1));

        var twoLevelQuoteConvention = new QuoteConvention(
            "two-level-quote-convention",
            [new SingleLevelQuoteConvention("\u201c", "\u201d"), new SingleLevelQuoteConvention("\u2018", "\u2019"),]
        );
        Assert.That(twoLevelQuoteConvention.NumLevels, Is.EqualTo(2));

        var threeLevelQuoteConvention = new QuoteConvention(
            "three-level-quote-convention",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201D", "\u201D"),
            ]
        );
        Assert.That(threeLevelQuoteConvention.NumLevels, Is.EqualTo(3));
    }

    [Test]
    public void GetOpeningQuoteAtLevel()
    {
        var quoteConvention = new QuoteConvention(
            "test-quote-convention",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
            ]
        );
        Assert.That(quoteConvention.GetOpeningQuotationMarkAtDepth(1), Is.EqualTo("\u201c"));
        Assert.That(quoteConvention.GetOpeningQuotationMarkAtDepth(2), Is.EqualTo("\u2018"));
        Assert.That(quoteConvention.GetOpeningQuotationMarkAtDepth(3), Is.EqualTo("\u00ab"));
    }

    [Test]
    public void GetClosingQuoteAtLevel()
    {
        var quoteConvention = new QuoteConvention(
            "test-quote-convention",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
            ]
        );
        Assert.That(quoteConvention.GetClosingQuotationMarkAtDepth(1), Is.EqualTo("\u201d"));
        Assert.That(quoteConvention.GetClosingQuotationMarkAtDepth(2), Is.EqualTo("\u2019"));
        Assert.That(quoteConvention.GetClosingQuotationMarkAtDepth(3), Is.EqualTo("\u00bb"));
    }

    [Test]
    public void GetExpectedQuotationMark()
    {
        var quoteConvention = new QuoteConvention(
            "test-quote-convention",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
            ]
        );
        Assert.That(quoteConvention.GetExpectedQuotationMark(1, QuotationMarkDirection.Opening), Is.EqualTo("\u201c"));
        Assert.That(quoteConvention.GetExpectedQuotationMark(1, QuotationMarkDirection.Closing), Is.EqualTo("\u201d"));
        Assert.That(quoteConvention.GetExpectedQuotationMark(2, QuotationMarkDirection.Opening), Is.EqualTo("\u2018"));
        Assert.That(quoteConvention.GetExpectedQuotationMark(2, QuotationMarkDirection.Closing), Is.EqualTo("\u2019"));
        Assert.That(quoteConvention.GetExpectedQuotationMark(3, QuotationMarkDirection.Opening), Is.EqualTo("\u00ab"));
        Assert.That(quoteConvention.GetExpectedQuotationMark(3, QuotationMarkDirection.Closing), Is.EqualTo("\u00bb"));
        Assert.That(quoteConvention.GetExpectedQuotationMark(4, QuotationMarkDirection.Opening), Is.EqualTo(""));
        Assert.That(quoteConvention.GetExpectedQuotationMark(4, QuotationMarkDirection.Closing), Is.EqualTo(""));
        Assert.That(quoteConvention.GetExpectedQuotationMark(0, QuotationMarkDirection.Opening), Is.EqualTo(""));
        Assert.That(quoteConvention.GetExpectedQuotationMark(0, QuotationMarkDirection.Closing), Is.EqualTo(""));
    }

    [Test]
    public void IncludesOpeningQuotationMark()
    {
        var emptyQuoteConvention = new QuoteConvention("empty quote convention", []);
        Assert.IsFalse(emptyQuoteConvention.IncludesOpeningQuotationMark("\u201c"));

        var positiveQuoteConvention1 = new QuoteConvention(
            "positive quote convention 1",
            [new SingleLevelQuoteConvention("\u201c", "\u201d")]
        );
        Assert.IsTrue(positiveQuoteConvention1.IncludesOpeningQuotationMark("\u201c"));

        var negativeQuoteConvention1 = new QuoteConvention(
            "negative quote convention 1",
            [new SingleLevelQuoteConvention("\u2018", "\u2019")]
        );
        Assert.IsFalse(negativeQuoteConvention1.IncludesOpeningQuotationMark("\u201c"));

        var negativeQuoteConvention2 = new QuoteConvention(
            "negative quote convention 2",
            [new SingleLevelQuoteConvention("\u201d", "\u201c")]
        );
        Assert.IsFalse(negativeQuoteConvention2.IncludesOpeningQuotationMark("\u201c"));

        var positiveQuoteConvention2 = new QuoteConvention(
            "positive quote convention 2",
            [new SingleLevelQuoteConvention("\u201c", "\u201d"), new SingleLevelQuoteConvention("\u2018", "\u2019")]
        );
        Assert.IsTrue(positiveQuoteConvention2.IncludesOpeningQuotationMark("\u201c"));

        var positiveQuoteConvention3 = new QuoteConvention(
            "positive quote convention 3",
            [new SingleLevelQuoteConvention("\u2018", "\u2019"), new SingleLevelQuoteConvention("\u201c", "\u201d")]
        );
        Assert.IsTrue(positiveQuoteConvention3.IncludesOpeningQuotationMark("\u201c"));

        var negativeQuoteConvention3 = new QuoteConvention(
            "negative quote convention 3",
            [
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("'", "'"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
            ]
        );
        Assert.IsFalse(negativeQuoteConvention3.IncludesOpeningQuotationMark("\u201c"));
    }

    [Test]
    public void IncludesClosingQuotationMark()
    {
        var emptyQuoteConvention = new QuoteConvention("empty quote convention", []);
        Assert.IsFalse(emptyQuoteConvention.IncludesClosingQuotationMark("\u201d"));

        var positiveQuoteConvention1 = new QuoteConvention(
            "positive quote convention 1",
            [new SingleLevelQuoteConvention("\u201c", "\u201d")]
        );
        Assert.IsTrue(positiveQuoteConvention1.IncludesClosingQuotationMark("\u201d"));

        var negativeQuoteConvention1 = new QuoteConvention(
            "negative quote convention 1",
            [new SingleLevelQuoteConvention("\u2018", "\u2019")]
        );
        Assert.IsFalse(negativeQuoteConvention1.IncludesClosingQuotationMark("\u201d"));

        var negativeQuoteConvention2 = new QuoteConvention(
            "negative quote convention 2",
            [new SingleLevelQuoteConvention("\u201d", "\u201c")]
        );
        Assert.IsFalse(negativeQuoteConvention2.IncludesClosingQuotationMark("\u201d"));

        var positiveQuoteConvention2 = new QuoteConvention(
            "positive quote convention 2",
            [new SingleLevelQuoteConvention("\u201c", "\u201d"), new SingleLevelQuoteConvention("\u2018", "\u2019")]
        );
        Assert.IsTrue(positiveQuoteConvention2.IncludesClosingQuotationMark("\u201d"));

        var positiveQuoteConvention3 = new QuoteConvention(
            "positive quote convention 3",
            [new SingleLevelQuoteConvention("\u2018", "\u2019"), new SingleLevelQuoteConvention("\u201c", "\u201d")]
        );
        Assert.IsTrue(positiveQuoteConvention3.IncludesClosingQuotationMark("\u201d"));

        var negativeQuoteConvention3 = new QuoteConvention(
            "negative quote convention 3",
            [
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("'", "'"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
            ]
        );
        Assert.IsFalse(negativeQuoteConvention3.IncludesClosingQuotationMark("\u201d"));
    }

    [Test]
    public void GetPossibleDepths()
    {
        var quoteConvention = new QuoteConvention(
            "test-quote-convention",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        Assert.That(quoteConvention.GetPossibleDepths("\u201c", QuotationMarkDirection.Opening).SequenceEqual([1, 3]));
        Assert.That(quoteConvention.GetPossibleDepths("\u201c", QuotationMarkDirection.Closing), Has.Count.EqualTo(0));
        Assert.That(quoteConvention.GetPossibleDepths("\u2018", QuotationMarkDirection.Opening).SequenceEqual([2, 4]));
        Assert.That(quoteConvention.GetPossibleDepths("\u2018", QuotationMarkDirection.Closing), Has.Count.EqualTo(0));
        Assert.That(quoteConvention.GetPossibleDepths("\u201d", QuotationMarkDirection.Opening), Has.Count.EqualTo(0));
        Assert.That(quoteConvention.GetPossibleDepths("\u201d", QuotationMarkDirection.Closing).SequenceEqual([1, 3]));
        Assert.That(quoteConvention.GetPossibleDepths("\u2019", QuotationMarkDirection.Opening), Has.Count.EqualTo(0));
        Assert.That(quoteConvention.GetPossibleDepths("\u2019", QuotationMarkDirection.Closing).SequenceEqual([2, 4]));
        Assert.That(quoteConvention.GetPossibleDepths("\u00ab", QuotationMarkDirection.Opening), Has.Count.EqualTo(0));
        Assert.That(quoteConvention.GetPossibleDepths("\u00ab", QuotationMarkDirection.Closing), Has.Count.EqualTo(0));
    }

    [Test]
    public void IsCompatibleWithObservedQuotationMarks()
    {
        var quoteConvention = new QuoteConvention(
            "test-quote-convention",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
            ]
        );
        Assert.IsTrue(
            quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201c", "\u2018"], ["\u201d", "\u2019"])
        );
        Assert.IsTrue(
            quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201c", "\u00ab"], ["\u201d", "\u00bb"])
        );
        Assert.IsTrue(quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201c"], ["\u201d", "\u2019"]));
        Assert.IsTrue(quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201c"], ["\u201d"]));
        Assert.IsTrue(
            quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201c", "\u00ab"], ["\u201d", "\u2019"])
        );

        Assert.IsFalse(quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201d", "\u2019"], ["\u201c"]));

        Assert.IsFalse(quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201c", "\u201e"], ["\u201d"]));

        Assert.IsFalse(
            quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201c", "\u2018"], ["\u201d", "\u201f"])
        );

        // must have observed the first-level quotes
        Assert.IsFalse(quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u2018"], ["\u201d"]));
        Assert.IsFalse(quoteConvention.IsCompatibleWithObservedQuotationMarks(["\u201c", "\u2018"], ["\u00ab"]));
    }

    [Test]
    public void Normalize()
    {
        var emptyQuoteConvention = new QuoteConvention("empty-quote-convention", []);
        var normalizedEmptyQuoteConvention = emptyQuoteConvention.Normalize();
        Assert.That(normalizedEmptyQuoteConvention.Name, Is.EqualTo("empty-quote-convention_normalized"));
        Assert.That(normalizedEmptyQuoteConvention.NumLevels, Is.EqualTo(0));

        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard-english-quote-convention",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var normalizedStandardEnglishQuoteConvention = standardEnglishQuoteConvention.Normalize();
        Assert.That(
            normalizedStandardEnglishQuoteConvention.Name,
            Is.EqualTo("standard-english-quote-convention_normalized")
        );
        Assert.That(normalizedStandardEnglishQuoteConvention.NumLevels, Is.EqualTo(4));
        Assert.That(normalizedStandardEnglishQuoteConvention.GetOpeningQuotationMarkAtDepth(1), Is.EqualTo("\""));
        Assert.That(normalizedStandardEnglishQuoteConvention.GetClosingQuotationMarkAtDepth(1), Is.EqualTo("\""));
        Assert.That(normalizedStandardEnglishQuoteConvention.GetOpeningQuotationMarkAtDepth(2), Is.EqualTo("'"));
        Assert.That(normalizedStandardEnglishQuoteConvention.GetClosingQuotationMarkAtDepth(2), Is.EqualTo("'"));
        Assert.That(normalizedStandardEnglishQuoteConvention.GetOpeningQuotationMarkAtDepth(3), Is.EqualTo("\""));
        Assert.That(normalizedStandardEnglishQuoteConvention.GetClosingQuotationMarkAtDepth(3), Is.EqualTo("\""));
        Assert.That(normalizedStandardEnglishQuoteConvention.GetOpeningQuotationMarkAtDepth(4), Is.EqualTo("'"));
        Assert.That(normalizedStandardEnglishQuoteConvention.GetClosingQuotationMarkAtDepth(4), Is.EqualTo("'"));

        var westernEuropeanQuoteConvention = new QuoteConvention(
            "test-quote-convention",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var normalizedWesternEuropeanQuoteConvention = westernEuropeanQuoteConvention.Normalize();
        Assert.That(normalizedWesternEuropeanQuoteConvention.Name, Is.EqualTo("test-quote-convention_normalized"));
        Assert.That(normalizedWesternEuropeanQuoteConvention.NumLevels, Is.EqualTo(3));
        Assert.That(normalizedWesternEuropeanQuoteConvention.GetOpeningQuotationMarkAtDepth(1), Is.EqualTo("\""));
        Assert.That(normalizedWesternEuropeanQuoteConvention.GetClosingQuotationMarkAtDepth(1), Is.EqualTo("\""));
        Assert.That(normalizedWesternEuropeanQuoteConvention.GetOpeningQuotationMarkAtDepth(2), Is.EqualTo("\""));
        Assert.That(normalizedWesternEuropeanQuoteConvention.GetClosingQuotationMarkAtDepth(2), Is.EqualTo("\""));
        Assert.That(normalizedWesternEuropeanQuoteConvention.GetOpeningQuotationMarkAtDepth(3), Is.EqualTo("'"));
        Assert.That(normalizedWesternEuropeanQuoteConvention.GetClosingQuotationMarkAtDepth(3), Is.EqualTo("'"));

        var hybridBritishTypewriterEnglishQuoteConvention = new QuoteConvention(
            "hybrid-british-typewriter-english-quote-convention",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("'", "'"),
                new SingleLevelQuoteConvention("\"", "\""),
            ]
        );

        var normalizedHybridBritishTypewriterEnglishQuoteConvention = (
            hybridBritishTypewriterEnglishQuoteConvention.Normalize()
        );
        Assert.IsTrue(
            normalizedHybridBritishTypewriterEnglishQuoteConvention.Name
                == "hybrid-british-typewriter-english-quote-convention_normalized"
        );
        Assert.That(normalizedHybridBritishTypewriterEnglishQuoteConvention.NumLevels, Is.EqualTo(3));
        Assert.That(
            normalizedHybridBritishTypewriterEnglishQuoteConvention.GetOpeningQuotationMarkAtDepth(1),
            Is.EqualTo("\"")
        );
        Assert.That(
            normalizedHybridBritishTypewriterEnglishQuoteConvention.GetClosingQuotationMarkAtDepth(1),
            Is.EqualTo("\"")
        );
        Assert.That(
            normalizedHybridBritishTypewriterEnglishQuoteConvention.GetOpeningQuotationMarkAtDepth(2),
            Is.EqualTo("'")
        );
        Assert.That(
            normalizedHybridBritishTypewriterEnglishQuoteConvention.GetClosingQuotationMarkAtDepth(2),
            Is.EqualTo("'")
        );
        Assert.That(
            normalizedHybridBritishTypewriterEnglishQuoteConvention.GetOpeningQuotationMarkAtDepth(3),
            Is.EqualTo("\"")
        );
        Assert.That(
            normalizedHybridBritishTypewriterEnglishQuoteConvention.GetClosingQuotationMarkAtDepth(3),
            Is.EqualTo("\"")
        );
    }
}
