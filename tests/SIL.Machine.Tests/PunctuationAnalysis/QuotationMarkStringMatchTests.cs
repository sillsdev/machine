using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class QuotationMarkStringMatchTests
{
    [Test]
    public void GetQuotationMark()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("quick brown fox").Build(),
            6,
            7
        );
        Assert.That(quotationMarkStringMatch.QuotationMark, Is.EqualTo("b"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("quick brown fox").Build(),
            6,
            10
        );
        Assert.That(quotationMarkStringMatch.QuotationMark, Is.EqualTo("brow"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(new TextSegment.Builder().SetText("q").Build(), 0, 1);
        Assert.That(quotationMarkStringMatch.QuotationMark, Is.EqualTo("q"));
    }

    [Test]
    public void IsValidOpeningQuotationMark()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standardEnglish",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);

        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201c").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.IsValidOpeningQuotationMark(standardEnglishQuoteConventionSet), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201d").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.IsValidOpeningQuotationMark(standardEnglishQuoteConventionSet), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201d\u201c").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.IsValidOpeningQuotationMark(standardEnglishQuoteConventionSet), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201d\u201c").Build(),
            0,
            2
        );
        Assert.That(quotationMarkStringMatch.IsValidOpeningQuotationMark(standardEnglishQuoteConventionSet), Is.False);
    }

    [Test]
    public void IsValidClosingQuotationMark()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standardEnglish",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);

        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201d").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.IsValidClosingQuotationMark(standardEnglishQuoteConventionSet), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201c").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.IsValidClosingQuotationMark(standardEnglishQuoteConventionSet), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201d\u201c").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.IsValidClosingQuotationMark(standardEnglishQuoteConventionSet), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201d\u201c").Build(),
            0,
            2
        );
        Assert.That(quotationMarkStringMatch.IsValidClosingQuotationMark(standardEnglishQuoteConventionSet), Is.False);
    }

    [Test]
    public void DoesQuotationMarkMatch()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.QuotationMarkMatches(new Regex(@"^s$", RegexOptions.Compiled)), Is.True);
        Assert.That(quotationMarkStringMatch.QuotationMarkMatches(new Regex(@"a", RegexOptions.Compiled)), Is.False);
        Assert.That(quotationMarkStringMatch.QuotationMarkMatches(new Regex(@"sa", RegexOptions.Compiled)), Is.False);
    }

    [Test]
    public void DoesNextCharacterMatch()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.NextCharacterMatches(new Regex(@"^s$", RegexOptions.Compiled)), Is.False);
        Assert.That(quotationMarkStringMatch.NextCharacterMatches(new Regex(@"a", RegexOptions.Compiled)), Is.True);
        Assert.That(quotationMarkStringMatch.NextCharacterMatches(new Regex(@"sa", RegexOptions.Compiled)), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.NextCharacterMatches(new Regex(@".*", RegexOptions.Compiled)), Is.False);
    }

    [Test]
    public void DoesPreviousCharacterMatch()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            1,
            2
        );
        Assert.That(
            quotationMarkStringMatch.PreviousCharacterMatches(new Regex(@"^s$", RegexOptions.Compiled)),
            Is.True
        );
        Assert.That(
            quotationMarkStringMatch.PreviousCharacterMatches(new Regex(@"a", RegexOptions.Compiled)),
            Is.False
        );
        Assert.That(
            quotationMarkStringMatch.PreviousCharacterMatches(new Regex(@"sa", RegexOptions.Compiled)),
            Is.False
        );

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(
            quotationMarkStringMatch.PreviousCharacterMatches(new Regex(@".*", RegexOptions.Compiled)),
            Is.False
        );
    }

    [Test]
    public void GetPreviousCharacter()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.PreviousCharacter, Is.EqualTo("s"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.PreviousCharacter, Is.EqualTo("x"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.PreviousCharacter, Is.Null);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201c\u201d").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.PreviousCharacter, Is.EqualTo("“"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder()
                .SetText("\"उत्पत्ति पुस्तकले")
                .SetPreviousSegment(new TextSegment.Builder().SetText("उत्पत्ति पुस्तकले").Build())
                .Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.PreviousCharacter, Is.EqualTo("\u0947"));
    }

    [Test]
    public void GetNextCharacter()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.NextCharacter, Is.EqualTo("m"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.NextCharacter, Is.EqualTo("a"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            10,
            11
        );

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201c\u201d").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.NextCharacter, Is.EqualTo("”"));
    }

    [Test]
    public void DoesLeadingSubstringMatch()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            5,
            6
        );
        Assert.That(
            quotationMarkStringMatch.LeadingSubstringMatches(new Regex(@"^sampl$", RegexOptions.Compiled)),
            Is.True
        );

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(
            quotationMarkStringMatch.LeadingSubstringMatches(new Regex(@".+", RegexOptions.Compiled)),
            Is.False
        );

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201c\u201d").Build(),
            1,
            2
        );
        Assert.That(
            quotationMarkStringMatch.LeadingSubstringMatches(new Regex(@"\u201c", RegexOptions.Compiled)),
            Is.True
        );
    }

    [Test]
    public void DoesTrailingSubstringMatch()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            5,
            6
        );
        Assert.That(
            quotationMarkStringMatch.TrailingSubstringMatches(new Regex(@"^ text$", RegexOptions.Compiled)),
            Is.True
        );

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            10,
            11
        );
        Assert.That(
            quotationMarkStringMatch.TrailingSubstringMatches(new Regex(@".+", RegexOptions.Compiled)),
            Is.False
        );

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201c\u201d").Build(),
            0,
            1
        );
        Assert.That(
            quotationMarkStringMatch.TrailingSubstringMatches(new Regex(@"\u201d", RegexOptions.Compiled)),
            Is.True
        );
    }

    [Test]
    public void GetContext()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("this is a bunch' of sample text").Build(),
            15,
            16
        );
        Assert.That(quotationMarkStringMatch.Context, Is.EqualTo("is a bunch' of sample"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("this is a bunch' of sample text").Build(),
            5,
            6
        );
        Assert.That(quotationMarkStringMatch.Context, Is.EqualTo("this is a bunch'"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("this is a bunch' of sample text").Build(),
            25,
            26
        );
        Assert.That(quotationMarkStringMatch.Context, Is.EqualTo("' of sample text"));

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("short").Build(),
            3,
            4
        );
        Assert.That(quotationMarkStringMatch.Context, Is.EqualTo("short"));
    }

    [Test]
    public void Resolve()
    {
        TextSegment textSegment = new TextSegment.Builder().SetText("'").Build();
        var quotationMarkStringMatch = new QuotationMarkStringMatch(textSegment, 0, 1);
        Assert.That(
            quotationMarkStringMatch.Resolve(2, QuotationMarkDirection.Opening),
            Is.EqualTo(new QuotationMarkMetadata("'", 2, QuotationMarkDirection.Opening, textSegment, 0, 1))
        );
        Assert.That(
            quotationMarkStringMatch.Resolve(1, QuotationMarkDirection.Opening),
            Is.EqualTo(new QuotationMarkMetadata("'", 1, QuotationMarkDirection.Opening, textSegment, 0, 1))
        );
        Assert.That(
            quotationMarkStringMatch.Resolve(1, QuotationMarkDirection.Closing),
            Is.EqualTo(new QuotationMarkMetadata("'", 1, QuotationMarkDirection.Closing, textSegment, 0, 1))
        );
    }

    [Test]
    public void IsAtStartOfSegment()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.IsAtStartOfSegment, Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.IsAtStartOfSegment, Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201csample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.IsAtStartOfSegment, Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            15,
            16
        );
        Assert.That(quotationMarkStringMatch.IsAtStartOfSegment, Is.False);
    }

    [Test]
    public void IsAtEndOfSegment()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.IsAtEndOfSegment, Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.IsAtEndOfSegment, Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201csample text\u201d").Build(),
            12,
            13
        );
        Assert.That(quotationMarkStringMatch.IsAtEndOfSegment, Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            15,
            16
        );
        Assert.That(quotationMarkStringMatch.IsAtEndOfSegment, Is.False);
    }

    [Test]
    public void HasLeadingWhitespace()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample\ttext").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").AddPrecedingMarker(UsfmMarkerType.Paragraph).Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").AddPrecedingMarker(UsfmMarkerType.Embed).Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").AddPrecedingMarker(UsfmMarkerType.Verse).Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").AddPrecedingMarker(UsfmMarkerType.Chapter).Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").AddPrecedingMarker(UsfmMarkerType.Character).Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201csample text").AddPrecedingMarker(UsfmMarkerType.Verse).Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingWhitespace(), Is.True);
    }

    [Test]
    public void HasTrailingWhitespace()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            5,
            6
        );
        Assert.That(quotationMarkStringMatch.HasTrailingWhitespace(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample\ttext").Build(),
            5,
            6
        );
        Assert.That(quotationMarkStringMatch.HasTrailingWhitespace(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.HasTrailingWhitespace(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").AddPrecedingMarker(UsfmMarkerType.Paragraph).Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.HasTrailingWhitespace(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").AddPrecedingMarker(UsfmMarkerType.Embed).Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.HasTrailingWhitespace(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").AddPrecedingMarker(UsfmMarkerType.Verse).Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.HasTrailingWhitespace(), Is.False);
    }

    [Test]
    public void HasLeadingPunctuation()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample)\u201d text").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasLeadingPunctuation(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample) \u201d text").Build(),
            8,
            9
        );
        Assert.That(quotationMarkStringMatch.HasLeadingPunctuation(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample,\u201d text").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasLeadingPunctuation(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample.\u201d text").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasLeadingPunctuation(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("\u201csample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingPunctuation(), Is.False);
    }

    [Test]
    public void HasTrailingPunctuation()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample \u201c-text").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasTrailingPunctuation(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample \u201c text").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasTrailingPunctuation(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text\u201d").Build(),
            11,
            12
        );
        Assert.That(quotationMarkStringMatch.HasTrailingPunctuation(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample', text\u201d").Build(),
            6,
            7
        );
        Assert.That(quotationMarkStringMatch.HasTrailingPunctuation(), Is.True);
    }

    [Test]
    public void HasLetterInLeadingSubstring()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.HasLetterInLeadingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("ꮪample text").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.HasLetterInLeadingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLetterInLeadingSubstring(), Is.False);
    }

    [Test]
    public void HasLetterInTrailingSubstring()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            9,
            10
        );
        Assert.That(quotationMarkStringMatch.HasLetterInTrailingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample tex𑢼").Build(),
            9,
            10
        );
        Assert.That(quotationMarkStringMatch.HasLetterInTrailingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.HasLetterInTrailingSubstring(), Is.False);
    }

    [Test]
    public void HasLeadingLatinLetter()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.HasLeadingLatinLetter(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("5ample text").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.HasLeadingLatinLetter(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("Ｓample text").Build(),
            1,
            2
        );
        Assert.That(quotationMarkStringMatch.HasLeadingLatinLetter(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            0,
            1
        );
        Assert.That(quotationMarkStringMatch.HasLeadingLatinLetter(), Is.False);
    }

    [Test]
    public void HasTrailingLatinLetter()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            9,
            10
        );
        Assert.That(quotationMarkStringMatch.HasTrailingLatinLetter(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample texＴ").Build(),
            9,
            10
        );
        Assert.That(quotationMarkStringMatch.HasTrailingLatinLetter(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample text").Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.HasTrailingLatinLetter(), Is.False);
    }

    [Test]
    public void HasQuoteIntroducerInLeadingSubstring()
    {
        var quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample, \u201ctext").Build(),
            8,
            9
        );
        Assert.That(quotationMarkStringMatch.HasQuoteIntroducerInLeadingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample,\u201ctext").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasQuoteIntroducerInLeadingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample: \u201ctext").Build(),
            8,
            9
        );
        Assert.That(quotationMarkStringMatch.HasQuoteIntroducerInLeadingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample:\u201ctext").Build(),
            7,
            8
        );
        Assert.That(quotationMarkStringMatch.HasQuoteIntroducerInLeadingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample,  \u201ctext").Build(),
            9,
            10
        );
        Assert.That(quotationMarkStringMatch.HasQuoteIntroducerInLeadingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample,, \u201ctext").Build(),
            9,
            10
        );
        Assert.That(quotationMarkStringMatch.HasQuoteIntroducerInLeadingSubstring(), Is.True);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample, a \u201ctext").Build(),
            10,
            11
        );
        Assert.That(quotationMarkStringMatch.HasQuoteIntroducerInLeadingSubstring(), Is.False);

        quotationMarkStringMatch = new QuotationMarkStringMatch(
            new TextSegment.Builder().SetText("sample, text").Build(),
            8,
            9
        );
        Assert.That(quotationMarkStringMatch.HasQuoteIntroducerInLeadingSubstring(), Is.True);
    }
}
