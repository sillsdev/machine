using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class QuoteConventionSetTests
{
    [Test]
    public void QuoteRegexes()
    {
        var emptyQuoteConventionSet = new QuoteConventionSet([]);
        Assert.That(
            emptyQuoteConventionSet.OpeningQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            emptyQuoteConventionSet.ClosingQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            emptyQuoteConventionSet.AllQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"", RegexOptions.Compiled).ToString())
        );

        var quoteConventionSetWithEmptyConventions = new QuoteConventionSet(
            [new QuoteConvention("empty convention 1", []), new QuoteConvention("empty convention 2", [])]
        );
        Assert.That(
            quoteConventionSetWithEmptyConventions.OpeningQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            quoteConventionSetWithEmptyConventions.ClosingQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            quoteConventionSetWithEmptyConventions.AllQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"", RegexOptions.Compiled).ToString())
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet(
            [
                new QuoteConvention(
                    "standard_english",
                    [
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    ]
                )
            ]
        );
        Assert.That(
            standardEnglishQuoteConventionSet.OpeningQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[‘“]", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            standardEnglishQuoteConventionSet.ClosingQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[’”]", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            standardEnglishQuoteConventionSet.AllQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[‘’“”]", RegexOptions.Compiled).ToString())
        );

        var westernEuropeanQuoteConventionSet = new QuoteConventionSet(
            [
                new QuoteConvention(
                    "western_european",
                    [
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    ]
                ),
            ]
        );
        Assert.That(
            westernEuropeanQuoteConventionSet.OpeningQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[‘“«]", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            westernEuropeanQuoteConventionSet.ClosingQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[’”»]", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            westernEuropeanQuoteConventionSet.AllQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[‘’“”«»]", RegexOptions.Compiled).ToString())
        );

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [
                new QuoteConvention(
                    "standard_english",
                    [
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    ]
                ),
                new QuoteConvention(
                    "typewriter_french",
                    [
                        new SingleLevelQuoteConvention("<<", ">>"),
                        new SingleLevelQuoteConvention("<", ">"),
                        new SingleLevelQuoteConvention("<<", ">>"),
                        new SingleLevelQuoteConvention("<", ">"),
                    ]
                ),
                new QuoteConvention(
                    "standard_french",
                    [
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u2039", "\u203a"),
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u2039", "\u203a"),
                    ]
                ),
            ]
        );
        Assert.That(
            multipleQuoteConventionSet.OpeningQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[‘‹“«<<<]", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            multipleQuoteConventionSet.ClosingQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[’›”»>>>]", RegexOptions.Compiled).ToString())
        );
        Assert.That(
            multipleQuoteConventionSet.AllQuotationMarkRegex.ToString(),
            Is.EqualTo(new Regex(@"[‘’‹›“”«»<<<>>>]", RegexOptions.Compiled).ToString())
        );
    }

    [Test]
    public void QuotationMarkPairMap()
    {
        var emptyQuoteConventionSet = new QuoteConventionSet([]);
        Assert.That(emptyQuoteConventionSet.OpeningMarksByClosingMark, Has.Count.EqualTo(0));
        Assert.That(emptyQuoteConventionSet.ClosingMarksByOpeningMark, Has.Count.EqualTo(0));

        var quoteConventionSetWithEmptyConventions = new QuoteConventionSet(
            [new QuoteConvention("empty convention 1", []), new QuoteConvention("empty convention 2", [])]
        );
        Assert.That(quoteConventionSetWithEmptyConventions.OpeningMarksByClosingMark, Has.Count.EqualTo(0));
        Assert.That(quoteConventionSetWithEmptyConventions.ClosingMarksByOpeningMark, Has.Count.EqualTo(0));

        var standardEnglishQuoteConventionSet = new QuoteConventionSet(
            [
                new QuoteConvention(
                    "standard_english",
                    [
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    ]
                )
            ]
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .OpeningMarksByClosingMark.OrderBy(kvp => kvp.Key)
                .SequenceEqual(
                    new Dictionary<string, HashSet<string>> { { "’", ["‘"] }, { "”", ["“"] } }.OrderBy(kvp => kvp.Key),
                    new QuotationMarkPairMapEqualityComparer()
                )
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .ClosingMarksByOpeningMark.OrderBy(kvp => kvp.Key)
                .SequenceEqual(
                    new Dictionary<string, HashSet<string>> { { "‘", ["’"] }, { "“", ["”"] } }.OrderBy(kvp => kvp.Key),
                    new QuotationMarkPairMapEqualityComparer()
                )
        );

        var westernEuropeanQuoteConventionSet = new QuoteConventionSet(
            [
                new QuoteConvention(
                    "western_european",
                    [
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    ]
                ),
            ]
        );
        Assert.That(
            westernEuropeanQuoteConventionSet
                .OpeningMarksByClosingMark.OrderBy(kvp => kvp.Key)
                .SequenceEqual(
                    new Dictionary<string, HashSet<string>>
                    {
                        { "’", ["‘"] },
                        { "”", ["“"] },
                        { "»", ["«"] }
                    }.OrderBy(kvp => kvp.Key),
                    new QuotationMarkPairMapEqualityComparer()
                )
        );
        Assert.That(
            westernEuropeanQuoteConventionSet
                .ClosingMarksByOpeningMark.OrderBy(kvp => kvp.Key)
                .SequenceEqual(
                    new Dictionary<string, HashSet<string>>
                    {
                        { "‘", ["’"] },
                        { "“", ["”"] },
                        { "«", ["»"] }
                    }.OrderBy(kvp => kvp.Key),
                    new QuotationMarkPairMapEqualityComparer()
                )
        );

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [
                new QuoteConvention(
                    "standard_english",
                    [
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    ]
                ),
                new QuoteConvention(
                    "central_european",
                    [
                        new SingleLevelQuoteConvention("\u201e", "\u201c"),
                        new SingleLevelQuoteConvention("\u201a", "\u2018"),
                        new SingleLevelQuoteConvention("\u201e", "\u201c"),
                        new SingleLevelQuoteConvention("\u201a", "\u2018"),
                    ]
                ),
                new QuoteConvention(
                    "standard_swedish",
                    [
                        new SingleLevelQuoteConvention("\u201d", "\u201d"),
                        new SingleLevelQuoteConvention("\u2019", "\u2019"),
                        new SingleLevelQuoteConvention("\u201d", "\u201d"),
                        new SingleLevelQuoteConvention("\u2019", "\u2019"),
                    ]
                ),
            ]
        );
        Assert.That(
            multipleQuoteConventionSet
                .ClosingMarksByOpeningMark.OrderBy(kvp => kvp.Key)
                .SequenceEqual(
                    new Dictionary<string, HashSet<string>>
                    {
                        { "‘", ["’"] },
                        { "“", ["”"] },
                        { "„", ["“"] },
                        { "‚", ["‘"] },
                        { "”", ["”"] },
                        { "’", ["’"] },
                    }.OrderBy(kvp => kvp.Key),
                    new QuotationMarkPairMapEqualityComparer()
                )
        );
        Assert.That(
            multipleQuoteConventionSet
                .OpeningMarksByClosingMark.OrderBy(kvp => kvp.Key)
                .SequenceEqual(
                    new Dictionary<string, HashSet<string>>
                    {
                        { "’", ["‘", "’"] },
                        { "”", ["“", "”"] },
                        { "“", ["„"] },
                        { "‘", ["‚"] },
                    }.OrderBy(kvp => kvp.Key),
                    new QuotationMarkPairMapEqualityComparer()
                )
        );
    }

    [Test]
    public void GetQuoteConventionByName()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var centralEuropeanQuoteConvention = new QuoteConvention(
            "central_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );
        var multipleQuoteConventionSet = new QuoteConventionSet(
            [standardEnglishQuoteConvention, centralEuropeanQuoteConvention, standardSwedishQuoteConvention]
        );

        Assert.That(
            multipleQuoteConventionSet.GetQuoteConventionByName("standard_english"),
            Is.EqualTo(standardEnglishQuoteConvention)
        );
        Assert.That(
            multipleQuoteConventionSet.GetQuoteConventionByName("central_european"),
            Is.EqualTo(centralEuropeanQuoteConvention)
        );
        Assert.That(
            multipleQuoteConventionSet.GetQuoteConventionByName("standard_swedish"),
            Is.EqualTo(standardSwedishQuoteConvention)
        );
        Assert.IsNull(multipleQuoteConventionSet.GetQuoteConventionByName("undefined convention"));
    }

    [Test]
    public void GetAllQuoteConventionNames()
    {
        Assert.That(new QuoteConventionSet([]).GetAllQuoteConventionNames(), Has.Count.EqualTo(0));
        Assert.That(
            new QuoteConventionSet([new QuoteConvention("conv", [])])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["conv"])
        );
        Assert.That(
            new QuoteConventionSet([new QuoteConvention("conv1", []), new QuoteConvention("conv2", [])])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["conv1", "conv2"])
        );
        Assert.That(
            new QuoteConventionSet([new QuoteConvention("conv2", []), new QuoteConvention("conv1", [])])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["conv1", "conv2"])
        );
    }

    [Test]
    public void GetPossibleOpeningQuotationMarks()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var centralEuropeanQuoteConvention = new QuoteConvention(
            "central_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.That(standardEnglishQuoteConventionSet.GetPossibleOpeningQuotationMarks().SequenceEqual(["‘", "“"]));

        var centralEuropeanQuoteConventionSet = new QuoteConventionSet([centralEuropeanQuoteConvention]);
        Assert.That(centralEuropeanQuoteConventionSet.GetPossibleOpeningQuotationMarks().SequenceEqual(["‚", "„"]));

        var standardSwedishQuoteConventionSet = new QuoteConventionSet([standardSwedishQuoteConvention]);
        Assert.That(standardSwedishQuoteConventionSet.GetPossibleOpeningQuotationMarks().SequenceEqual(["’", "”"]));

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [standardEnglishQuoteConvention, centralEuropeanQuoteConvention, standardSwedishQuoteConvention]
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleOpeningQuotationMarks().SequenceEqual(["‘", "’", "‚", "“", "”", "„"])
        );
    }

    [Test]
    public void GetPossibleClosingQuotationMarks()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );
        var centralEuropeanQuoteConvention = new QuoteConvention(
            "central_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.That(standardEnglishQuoteConventionSet.GetPossibleClosingQuotationMarks().SequenceEqual(["’", "”"]));

        var centralEuropeanQuoteConventionSet = new QuoteConventionSet([centralEuropeanQuoteConvention]);
        Assert.That(centralEuropeanQuoteConventionSet.GetPossibleClosingQuotationMarks().SequenceEqual(["‘", "“"]));

        var standardSwedishQuoteConventionSet = new QuoteConventionSet([standardSwedishQuoteConvention]);
        Assert.That(standardSwedishQuoteConventionSet.GetPossibleClosingQuotationMarks().SequenceEqual(["’", "”"]));

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [standardEnglishQuoteConvention, centralEuropeanQuoteConvention, standardSwedishQuoteConvention]
        );
        Assert.That(multipleQuoteConventionSet.GetPossibleClosingQuotationMarks().SequenceEqual(["‘", "’", "“", "”"]));
    }

    [Test]
    public void IsOpeningQuotationMark()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var centralEuropeanQuoteConvention = new QuoteConvention(
            "central_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var standardFrenchQuoteConvention = new QuoteConvention(
            "standard_french",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.IsTrue(standardEnglishQuoteConventionSet.IsValidOpeningQuotationMark("‘"));
        Assert.IsTrue(standardEnglishQuoteConventionSet.IsValidOpeningQuotationMark("“"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsValidOpeningQuotationMark("”"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsValidOpeningQuotationMark("’"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsValidOpeningQuotationMark(""));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsValidOpeningQuotationMark("‘“"));

        var centralEuropeanQuoteConventionSet = new QuoteConventionSet([centralEuropeanQuoteConvention]);
        Assert.IsTrue(centralEuropeanQuoteConventionSet.IsValidOpeningQuotationMark("‚"));
        Assert.IsTrue(centralEuropeanQuoteConventionSet.IsValidOpeningQuotationMark("„"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.IsValidOpeningQuotationMark("‘"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.IsValidOpeningQuotationMark("“"));

        var standardSwedishQuoteConventionSet = new QuoteConventionSet([standardSwedishQuoteConvention]);
        Assert.IsTrue(standardSwedishQuoteConventionSet.IsValidOpeningQuotationMark("’"));
        Assert.IsTrue(standardSwedishQuoteConventionSet.IsValidOpeningQuotationMark("”"));

        var standardFrenchQuoteConventionSet = new QuoteConventionSet([standardFrenchQuoteConvention]);
        Assert.IsTrue(standardFrenchQuoteConventionSet.IsValidOpeningQuotationMark("«"));
        Assert.IsTrue(standardFrenchQuoteConventionSet.IsValidOpeningQuotationMark("‹"));
        Assert.IsFalse(standardFrenchQuoteConventionSet.IsValidOpeningQuotationMark("»"));
        Assert.IsFalse(standardFrenchQuoteConventionSet.IsValidOpeningQuotationMark("›"));

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [
                standardEnglishQuoteConvention,
                centralEuropeanQuoteConvention,
                standardSwedishQuoteConvention,
                standardFrenchQuoteConvention,
            ]
        );
        Assert.That(
            multipleQuoteConventionSet
                .GetPossibleOpeningQuotationMarks()
                .SequenceEqual(["‘", "’", "‚", "‹", "“", "”", "„", "«"])
        );
        Assert.IsTrue(multipleQuoteConventionSet.IsValidOpeningQuotationMark("‘"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidOpeningQuotationMark("’"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidOpeningQuotationMark("‚"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidOpeningQuotationMark("“"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidOpeningQuotationMark("”"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidOpeningQuotationMark("„"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidOpeningQuotationMark("«"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidOpeningQuotationMark("‹"));
        Assert.IsFalse(multipleQuoteConventionSet.IsValidOpeningQuotationMark("»"));
        Assert.IsFalse(multipleQuoteConventionSet.IsValidOpeningQuotationMark("›"));
    }

    [Test]
    public void IsClosingQuotationMark()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var centralEuropeanQuoteConvention = new QuoteConvention(
            "central_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var standardFrenchQuoteConvention = new QuoteConvention(
            "standard_french",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.IsTrue(standardEnglishQuoteConventionSet.IsValidClosingQuotationMark("”"));
        Assert.IsTrue(standardEnglishQuoteConventionSet.IsValidClosingQuotationMark("’"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsValidClosingQuotationMark("‘"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsValidClosingQuotationMark("“"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsValidClosingQuotationMark(""));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsValidClosingQuotationMark("”’"));

        var centralEuropeanQuoteConventionSet = new QuoteConventionSet([centralEuropeanQuoteConvention]);
        Assert.IsTrue(centralEuropeanQuoteConventionSet.IsValidClosingQuotationMark("‘"));
        Assert.IsTrue(centralEuropeanQuoteConventionSet.IsValidClosingQuotationMark("“"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.IsValidClosingQuotationMark("„"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.IsValidClosingQuotationMark("‚"));

        var standardSwedishQuoteConventionSet = new QuoteConventionSet([standardSwedishQuoteConvention]);
        Assert.IsTrue(standardSwedishQuoteConventionSet.IsValidClosingQuotationMark("’"));
        Assert.IsTrue(standardSwedishQuoteConventionSet.IsValidClosingQuotationMark("”"));

        var standardFrenchQuoteConventionSet = new QuoteConventionSet([standardFrenchQuoteConvention]);
        Assert.IsTrue(standardFrenchQuoteConventionSet.IsValidClosingQuotationMark("»"));
        Assert.IsTrue(standardFrenchQuoteConventionSet.IsValidClosingQuotationMark("›"));
        Assert.IsFalse(standardFrenchQuoteConventionSet.IsValidClosingQuotationMark("«"));
        Assert.IsFalse(standardFrenchQuoteConventionSet.IsValidClosingQuotationMark("‹"));

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [
                standardEnglishQuoteConvention,
                centralEuropeanQuoteConvention,
                standardSwedishQuoteConvention,
                standardFrenchQuoteConvention,
            ]
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleClosingQuotationMarks().SequenceEqual(["‘", "’", "›", "“", "”", "»"])
        );
        Assert.IsTrue(multipleQuoteConventionSet.IsValidClosingQuotationMark("‘"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidClosingQuotationMark("’"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidClosingQuotationMark("“"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidClosingQuotationMark("”"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidClosingQuotationMark("»"));
        Assert.IsTrue(multipleQuoteConventionSet.IsValidClosingQuotationMark("›"));
        Assert.IsFalse(multipleQuoteConventionSet.IsValidClosingQuotationMark("«"));
        Assert.IsFalse(multipleQuoteConventionSet.IsValidClosingQuotationMark("‹"));
    }

    [Test]
    public void AreMarksAValidPair()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var centralEuropeanQuoteConvention = new QuoteConvention(
            "central_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var standardFrenchQuoteConvention = new QuoteConvention(
            "standard_french",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.IsTrue(standardEnglishQuoteConventionSet.MarksAreAValidPair("“", "”"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.MarksAreAValidPair("”", "“"));
        Assert.IsTrue(standardEnglishQuoteConventionSet.MarksAreAValidPair("‘", "’"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.MarksAreAValidPair("’", "‘"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.MarksAreAValidPair("‘", "”"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.MarksAreAValidPair("‘", "”"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.MarksAreAValidPair("‘", ""));
        Assert.IsFalse(standardEnglishQuoteConventionSet.MarksAreAValidPair("", ""));

        var centralEuropeanQuoteConventionSet = new QuoteConventionSet([centralEuropeanQuoteConvention]);
        Assert.IsTrue(centralEuropeanQuoteConventionSet.MarksAreAValidPair("„", "“"));
        Assert.IsTrue(centralEuropeanQuoteConventionSet.MarksAreAValidPair("‚", "‘"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.MarksAreAValidPair("“", "„"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.MarksAreAValidPair("’", "‚"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.MarksAreAValidPair("‚", "“"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.MarksAreAValidPair("‚", "’"));

        var standardSwedishQuoteConventionSet = new QuoteConventionSet([standardSwedishQuoteConvention]);
        Assert.IsTrue(standardSwedishQuoteConventionSet.MarksAreAValidPair("”", "”"));
        Assert.IsTrue(standardSwedishQuoteConventionSet.MarksAreAValidPair("’", "’"));
        Assert.IsFalse(standardSwedishQuoteConventionSet.MarksAreAValidPair("”", "’"));
        Assert.IsFalse(standardSwedishQuoteConventionSet.MarksAreAValidPair("’", "”"));

        var standardFrenchQuoteConventionSet = new QuoteConventionSet([standardFrenchQuoteConvention]);
        Assert.IsTrue(standardFrenchQuoteConventionSet.MarksAreAValidPair("«", "»"));
        Assert.IsTrue(standardFrenchQuoteConventionSet.MarksAreAValidPair("‹", "›"));
        Assert.IsFalse(standardFrenchQuoteConventionSet.MarksAreAValidPair("«", "›"));
        Assert.IsFalse(standardFrenchQuoteConventionSet.MarksAreAValidPair("‹", "»"));

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [
                standardEnglishQuoteConvention,
                centralEuropeanQuoteConvention,
                standardSwedishQuoteConvention,
                standardFrenchQuoteConvention,
            ]
        );
        Assert.IsTrue(multipleQuoteConventionSet.MarksAreAValidPair("“", "”"));
        Assert.IsTrue(multipleQuoteConventionSet.MarksAreAValidPair("‘", "’"));
        Assert.IsTrue(multipleQuoteConventionSet.MarksAreAValidPair("„", "“"));
        Assert.IsTrue(multipleQuoteConventionSet.MarksAreAValidPair("‚", "‘"));
        Assert.IsTrue(multipleQuoteConventionSet.MarksAreAValidPair("”", "”"));
        Assert.IsTrue(multipleQuoteConventionSet.MarksAreAValidPair("’", "’"));
        Assert.IsTrue(multipleQuoteConventionSet.MarksAreAValidPair("«", "»"));
        Assert.IsTrue(multipleQuoteConventionSet.MarksAreAValidPair("‹", "›"));
        Assert.IsFalse(multipleQuoteConventionSet.MarksAreAValidPair("‹", "»"));
        Assert.IsFalse(multipleQuoteConventionSet.MarksAreAValidPair("‹", "”"));
        Assert.IsFalse(multipleQuoteConventionSet.MarksAreAValidPair("„", "”"));
        Assert.IsFalse(multipleQuoteConventionSet.MarksAreAValidPair("’", "‘"));
    }

    [Test]
    public void IsQuotationMarkDirectionAmbiguous()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var typewriterEnglishQuoteConvention = new QuoteConvention(
            "typewriter_english",
            [
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
            ]
        );

        var centralEuropeanQuoteConvention = new QuoteConvention(
            "central_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var easternEuropeanQuoteConvention = new QuoteConvention(
            "eastern_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201d"),
                new SingleLevelQuoteConvention("\u201a", "\u2019"),
                new SingleLevelQuoteConvention("\u201e", "\u201d"),
                new SingleLevelQuoteConvention("\u201a", "\u2019"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("“"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("”"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("‘"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("’"));
        Assert.IsFalse(standardEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("\""));

        var typewriterEnglishQuoteConventionSet = new QuoteConventionSet([typewriterEnglishQuoteConvention]);
        Assert.IsTrue(typewriterEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("\""));
        Assert.IsTrue(typewriterEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("'"));
        Assert.IsFalse(typewriterEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("‘"));
        Assert.IsFalse(typewriterEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("’"));
        Assert.IsFalse(typewriterEnglishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("«"));

        var centralEuropeanQuoteConventionSet = new QuoteConventionSet([centralEuropeanQuoteConvention]);
        Assert.IsFalse(centralEuropeanQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("“"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("„"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("‘"));
        Assert.IsFalse(centralEuropeanQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("‚"));

        var standardSwedishQuoteConventionSet = new QuoteConventionSet([standardSwedishQuoteConvention]);
        Assert.IsTrue(standardSwedishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("”"));
        Assert.IsTrue(standardSwedishQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("’"));

        var easternEuropeanQuoteConventionSet = new QuoteConventionSet([easternEuropeanQuoteConvention]);
        Assert.IsFalse(easternEuropeanQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("”"));
        Assert.IsFalse(easternEuropeanQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("„"));
        Assert.IsFalse(easternEuropeanQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("’"));
        Assert.IsFalse(easternEuropeanQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("‚"));

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [
                standardEnglishQuoteConvention,
                typewriterEnglishQuoteConvention,
                centralEuropeanQuoteConvention,
                standardSwedishQuoteConvention,
                easternEuropeanQuoteConvention,
            ]
        );
        Assert.IsTrue(multipleQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("\""));
        Assert.IsTrue(multipleQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("'"));
        Assert.IsTrue(multipleQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("”"));
        Assert.IsTrue(multipleQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("’"));
        Assert.IsFalse(multipleQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("„"));
        Assert.IsFalse(multipleQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("‚"));

        // these are unambiguous because they are never the opening and closing in the same convention
        Assert.IsFalse(multipleQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("“"));
        Assert.IsFalse(multipleQuoteConventionSet.IsQuotationMarkDirectionAmbiguous("‘"));
    }

    [Test]
    public void GetPossiblePairedQuotationMarks()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var centralEuropeanQuoteConvention = new QuoteConvention(
            "central_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
                new SingleLevelQuoteConvention("\u201e", "\u201c"),
                new SingleLevelQuoteConvention("\u201a", "\u2018"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var easternEuropeanQuoteConvention = new QuoteConvention(
            "eastern_european",
            [
                new SingleLevelQuoteConvention("\u201e", "\u201d"),
                new SingleLevelQuoteConvention("\u201a", "\u2019"),
                new SingleLevelQuoteConvention("\u201e", "\u201d"),
                new SingleLevelQuoteConvention("\u201a", "\u2019"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.That(standardEnglishQuoteConventionSet.GetPossiblePairedQuotationMarks("“").SequenceEqual(["”"]));
        Assert.That(standardEnglishQuoteConventionSet.GetPossiblePairedQuotationMarks("”").SequenceEqual(["“"]));
        Assert.That(standardEnglishQuoteConventionSet.GetPossiblePairedQuotationMarks("‘").SequenceEqual(["’"]));
        Assert.That(standardEnglishQuoteConventionSet.GetPossiblePairedQuotationMarks("’").SequenceEqual(["‘"]));

        var centralEuropeanQuoteConventionSet = new QuoteConventionSet([centralEuropeanQuoteConvention]);
        Assert.That(centralEuropeanQuoteConventionSet.GetPossiblePairedQuotationMarks("„").SequenceEqual(["“"]));
        Assert.That(centralEuropeanQuoteConventionSet.GetPossiblePairedQuotationMarks("“").SequenceEqual(["„"]));
        Assert.That(centralEuropeanQuoteConventionSet.GetPossiblePairedQuotationMarks("‚").SequenceEqual(["‘"]));
        Assert.That(centralEuropeanQuoteConventionSet.GetPossiblePairedQuotationMarks("‘").SequenceEqual(["‚"]));

        var standardSwedishQuoteConventionSet = new QuoteConventionSet([standardSwedishQuoteConvention]);
        Assert.That(standardSwedishQuoteConventionSet.GetPossiblePairedQuotationMarks("”").SequenceEqual(["”"]));
        Assert.That(standardSwedishQuoteConventionSet.GetPossiblePairedQuotationMarks("’").SequenceEqual(["’"]));

        var easternEuropeanQuoteConventionSet = new QuoteConventionSet([easternEuropeanQuoteConvention]);
        Assert.That(easternEuropeanQuoteConventionSet.GetPossiblePairedQuotationMarks("„").SequenceEqual(["”"]));
        Assert.That(easternEuropeanQuoteConventionSet.GetPossiblePairedQuotationMarks("”").SequenceEqual(["„"]));
        Assert.That(easternEuropeanQuoteConventionSet.GetPossiblePairedQuotationMarks("‚").SequenceEqual(["’"]));
        Assert.That(easternEuropeanQuoteConventionSet.GetPossiblePairedQuotationMarks("’").SequenceEqual(["‚"]));

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [
                standardEnglishQuoteConvention,
                centralEuropeanQuoteConvention,
                standardSwedishQuoteConvention,
                easternEuropeanQuoteConvention,
            ]
        );
        Assert.That(multipleQuoteConventionSet.GetPossiblePairedQuotationMarks("“").SequenceEqual(["”", "„"]));
        Assert.That(multipleQuoteConventionSet.GetPossiblePairedQuotationMarks("”").SequenceEqual(["”", "“", "„"]));
        Assert.That(multipleQuoteConventionSet.GetPossiblePairedQuotationMarks("‘").SequenceEqual(["’", "‚"]));
        Assert.That(multipleQuoteConventionSet.GetPossiblePairedQuotationMarks("’").SequenceEqual(["’", "‘", "‚"]));
        Assert.That(multipleQuoteConventionSet.GetPossiblePairedQuotationMarks("„").SequenceEqual(["“", "”"]));
        Assert.That(multipleQuoteConventionSet.GetPossiblePairedQuotationMarks("‚").SequenceEqual(["‘", "’"]));
    }

    [Test]
    public void GetPossibleDepths()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var britishEnglishQuoteConvention = new QuoteConvention(
            "british_english",
            [
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
            ]
        );

        var normalizedWesternEuropeanQuoteConvention = new QuoteConvention(
            "westernEuropeanNormalized",
            [
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("\"", "\""),
                new SingleLevelQuoteConvention("'", "'"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.That(
            standardEnglishQuoteConventionSet
                .GetPossibleDepths("\u201c", QuotationMarkDirection.Opening)
                .SequenceEqual([1, 3])
        );
        Assert.That(
            standardEnglishQuoteConventionSet.GetPossibleDepths("\u201c", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .GetPossibleDepths("\u201d", QuotationMarkDirection.Closing)
                .SequenceEqual([1, 3])
        );
        Assert.That(
            standardEnglishQuoteConventionSet.GetPossibleDepths("\u201d", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .GetPossibleDepths("\u2018", QuotationMarkDirection.Opening)
                .SequenceEqual([2, 4])
        );
        Assert.That(
            standardEnglishQuoteConventionSet.GetPossibleDepths("\u2018", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .GetPossibleDepths("\u2019", QuotationMarkDirection.Closing)
                .SequenceEqual([2, 4])
        );
        Assert.That(
            standardEnglishQuoteConventionSet.GetPossibleDepths("\u2019", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet.GetPossibleDepths("\u201e", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet.GetPossibleDepths("\u201e", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet.GetPossibleDepths("\"", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet.GetPossibleDepths("\"", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );

        var britishEnglishQuoteConventionSet = new QuoteConventionSet([britishEnglishQuoteConvention]);
        Assert.That(
            britishEnglishQuoteConventionSet
                .GetPossibleDepths("\u2018", QuotationMarkDirection.Opening)
                .SequenceEqual([1, 3])
        );
        Assert.That(
            britishEnglishQuoteConventionSet.GetPossibleDepths("\u2018", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            britishEnglishQuoteConventionSet
                .GetPossibleDepths("\u2019", QuotationMarkDirection.Closing)
                .SequenceEqual([1, 3])
        );
        Assert.That(
            britishEnglishQuoteConventionSet.GetPossibleDepths("\u2019", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            britishEnglishQuoteConventionSet
                .GetPossibleDepths("\u201c", QuotationMarkDirection.Opening)
                .SequenceEqual([2, 4])
        );
        Assert.That(
            britishEnglishQuoteConventionSet.GetPossibleDepths("\u201c", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            britishEnglishQuoteConventionSet
                .GetPossibleDepths("\u201d", QuotationMarkDirection.Closing)
                .SequenceEqual([2, 4])
        );
        Assert.That(
            britishEnglishQuoteConventionSet.GetPossibleDepths("\u201d", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            britishEnglishQuoteConventionSet.GetPossibleDepths("\u201e", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            britishEnglishQuoteConventionSet.GetPossibleDepths("\u201e", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            britishEnglishQuoteConventionSet.GetPossibleDepths("'", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            britishEnglishQuoteConventionSet.GetPossibleDepths("'", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );

        var normalizedWesternEuropeanQuoteConventionSet = new QuoteConventionSet(
            [normalizedWesternEuropeanQuoteConvention]
        );
        Assert.That(
            normalizedWesternEuropeanQuoteConventionSet
                .GetPossibleDepths("\"", QuotationMarkDirection.Opening)
                .SequenceEqual([1, 2])
        );
        Assert.That(
            normalizedWesternEuropeanQuoteConventionSet
                .GetPossibleDepths("\"", QuotationMarkDirection.Closing)
                .SequenceEqual([1, 2])
        );
        Assert.That(
            normalizedWesternEuropeanQuoteConventionSet
                .GetPossibleDepths("'", QuotationMarkDirection.Opening)
                .SequenceEqual([3])
        );
        Assert.That(
            normalizedWesternEuropeanQuoteConventionSet
                .GetPossibleDepths("'", QuotationMarkDirection.Closing)
                .SequenceEqual([3])
        );
        Assert.That(
            normalizedWesternEuropeanQuoteConventionSet.GetPossibleDepths("\u201c", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            normalizedWesternEuropeanQuoteConventionSet.GetPossibleDepths("\u201c", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [standardEnglishQuoteConvention, britishEnglishQuoteConvention, normalizedWesternEuropeanQuoteConvention,]
        );
        Assert.That(
            multipleQuoteConventionSet
                .GetPossibleDepths("\u201c", QuotationMarkDirection.Opening)
                .OrderBy(d => d)
                .SequenceEqual([1, 2, 3, 4])
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("\u201c", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            multipleQuoteConventionSet
                .GetPossibleDepths("\u201d", QuotationMarkDirection.Closing)
                .OrderBy(d => d)
                .SequenceEqual([1, 2, 3, 4])
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("\u201d", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            multipleQuoteConventionSet
                .GetPossibleDepths("\u2018", QuotationMarkDirection.Opening)
                .OrderBy(d => d)
                .SequenceEqual([1, 2, 3, 4])
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("\u2018", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            multipleQuoteConventionSet
                .GetPossibleDepths("\u2019", QuotationMarkDirection.Closing)
                .OrderBy(d => d)
                .SequenceEqual([1, 2, 3, 4])
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("\u2019", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("\u201e", QuotationMarkDirection.Opening),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("\u201e", QuotationMarkDirection.Closing),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("\"", QuotationMarkDirection.Opening).SequenceEqual([1, 2])
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("\"", QuotationMarkDirection.Closing).SequenceEqual([1, 2])
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("'", QuotationMarkDirection.Opening).SequenceEqual([3])
        );
        Assert.That(
            multipleQuoteConventionSet.GetPossibleDepths("'", QuotationMarkDirection.Closing).SequenceEqual([3])
        );
    }

    [Test]
    public void DoesMetadataMatchQuotationMark()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.IsTrue(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201c", 1, QuotationMarkDirection.Opening)
        );
        Assert.IsTrue(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201c", 3, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201c", 2, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201c", 4, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201c", 1, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201c", 2, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201c", 3, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201c", 4, QuotationMarkDirection.Closing)
        );
        Assert.IsTrue(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201d", 1, QuotationMarkDirection.Closing)
        );
        Assert.IsTrue(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201d", 3, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201d", 2, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201d", 4, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201d", 1, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201d", 2, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201d", 3, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201d", 4, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2018", 1, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2018", 3, QuotationMarkDirection.Opening)
        );
        Assert.IsTrue(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2018", 2, QuotationMarkDirection.Opening)
        );
        Assert.IsTrue(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2018", 4, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2018", 1, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2018", 2, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2018", 3, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2018", 4, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2019", 1, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2019", 3, QuotationMarkDirection.Closing)
        );
        Assert.IsTrue(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2019", 2, QuotationMarkDirection.Closing)
        );
        Assert.IsTrue(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2019", 4, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2019", 1, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2019", 2, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2019", 3, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u2019", 4, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201e", 1, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201e", 1, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201e", 2, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201e", 2, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201e", 3, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201e", 3, QuotationMarkDirection.Closing)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201e", 4, QuotationMarkDirection.Opening)
        );
        Assert.IsFalse(
            standardEnglishQuoteConventionSet.MetadataMatchesQuotationMark("\u201e", 4, QuotationMarkDirection.Closing)
        );
    }

    [Test]
    public void FilterToCompatibleQuoteConventions()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var standardFrenchQuoteConvention = new QuoteConvention(
            "standard_french",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
            ]
        );

        var westernEuropeanQuoteConvention = new QuoteConvention(
            "western_european",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var standardSwedishQuoteConvention = new QuoteConvention(
            "standard_swedish",
            [
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
                new SingleLevelQuoteConvention("\u201d", "\u201d"),
                new SingleLevelQuoteConvention("\u2019", "\u2019"),
            ]
        );

        var standardEnglishQuoteConventionSet = new QuoteConventionSet([standardEnglishQuoteConvention]);
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c"], ["\u201d"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_english"])
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c", "\u2018"], ["\u201d", "\u2019"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_english"])
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c", "\u2018"], ["\u201d"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_english"])
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c"], ["\u201d", "\u2019"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_english"])
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u2018"], ["\u201d"])
                .GetAllQuoteConventionNames(),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c"], ["\u2019"])
                .GetAllQuoteConventionNames(),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201d"], ["\u201c"])
                .GetAllQuoteConventionNames(),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c", "\u201d"], ["\u201d"])
                .GetAllQuoteConventionNames(),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c", "\u201e"], ["\u201d"])
                .GetAllQuoteConventionNames(),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuoteConventionSet.FilterToCompatibleQuoteConventions([], []).GetAllQuoteConventionNames(),
            Has.Count.EqualTo(0)
        );

        var multipleQuoteConventionSet = new QuoteConventionSet(
            [
                standardEnglishQuoteConvention,
                standardFrenchQuoteConvention,
                westernEuropeanQuoteConvention,
                standardSwedishQuoteConvention,
            ]
        );
        Assert.That(
            multipleQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c"], ["\u201d"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_english"])
        );
        Assert.That(
            multipleQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c", "\u2018"], ["\u201d", "\u2019"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_english"])
        );
        Assert.That(
            multipleQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201d"], ["\u201d"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_swedish"])
        );
        Assert.That(
            multipleQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u201c"], ["\u201c"])
                .GetAllQuoteConventionNames(),
            Has.Count.EqualTo(0)
        );
        Assert.That(
            multipleQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u00ab"], ["\u00bb"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_french", "western_european"])
        );
        Assert.That(
            multipleQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u00ab", "\u2039"], ["\u00bb"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["standard_french"])
        );
        Assert.That(
            multipleQuoteConventionSet
                .FilterToCompatibleQuoteConventions(["\u00ab"], ["\u00bb", "\u201d"])
                .GetAllQuoteConventionNames()
                .SequenceEqual(["western_european"])
        );
        Assert.That(
            multipleQuoteConventionSet.FilterToCompatibleQuoteConventions([], []).GetAllQuoteConventionNames(),
            Has.Count.EqualTo(0)
        );
    }

    [Test]
    public void FindMostSimilarConvention()
    {
        var standardEnglishQuoteConvention = new QuoteConvention(
            "standard_english",
            [
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var standardFrenchQuoteConvention = new QuoteConvention(
            "standard_french",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u2039", "\u203a"),
            ]
        );

        var westernEuropeanQuoteConvention = new QuoteConvention(
            "western_european",
            [
                new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                new SingleLevelQuoteConvention("\u201c", "\u201d"),
                new SingleLevelQuoteConvention("\u2018", "\u2019"),
            ]
        );

        var allThreeQuoteConventionSet = new QuoteConventionSet(
            [standardEnglishQuoteConvention, standardFrenchQuoteConvention, westernEuropeanQuoteConvention,]
        );
        var twoFrenchQuoteConventionSet = new QuoteConventionSet(
            [westernEuropeanQuoteConvention, standardFrenchQuoteConvention]
        );

        var multipleEnglishQuotesTabulator = new QuotationMarkTabulator();
        multipleEnglishQuotesTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u2018",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    5,
                    6
                ),
                new QuotationMarkMetadata(
                    "\u2019",
                    2,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    13,
                    14
                ),
                new QuotationMarkMetadata(
                    "\u201d",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    14,
                    15
                ),
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    28,
                    29
                ),
                new QuotationMarkMetadata(
                    "\u201d",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    42,
                    43
                ),
            ]
        );
        Assert.That(
            allThreeQuoteConventionSet.FindMostSimilarConvention(multipleEnglishQuotesTabulator),
            Is.EqualTo((standardEnglishQuoteConvention, 1.0))
        );

        var multipleWesternEuropeanQuotesTabulator = new QuotationMarkTabulator();
        multipleWesternEuropeanQuotesTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u00ab",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u201c",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    5,
                    6
                ),
                new QuotationMarkMetadata(
                    "\u201d",
                    2,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    13,
                    14
                ),
                new QuotationMarkMetadata(
                    "\u00bb",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    14,
                    15
                ),
                new QuotationMarkMetadata(
                    "\u00ab",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    28,
                    29
                ),
                new QuotationMarkMetadata(
                    "\u00bb",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    42,
                    43
                ),
            ]
        );
        Assert.That(
            allThreeQuoteConventionSet.FindMostSimilarConvention(multipleWesternEuropeanQuotesTabulator),
            Is.EqualTo((westernEuropeanQuoteConvention, 1.0))
        );

        var multipleFrenchQuotesTabulator = new QuotationMarkTabulator();
        multipleFrenchQuotesTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u00ab",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u2039",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    5,
                    6
                ),
                new QuotationMarkMetadata(
                    "\u203a",
                    2,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    13,
                    14
                ),
                new QuotationMarkMetadata(
                    "\u00bb",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    14,
                    15
                ),
                new QuotationMarkMetadata(
                    "\u00ab",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    28,
                    29
                ),
                new QuotationMarkMetadata(
                    "\u00bb",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    42,
                    43
                ),
            ]
        );
        Assert.That(
            allThreeQuoteConventionSet.FindMostSimilarConvention(multipleFrenchQuotesTabulator),
            Is.EqualTo((standardFrenchQuoteConvention, 1.0))
        );
        Assert.That(
            twoFrenchQuoteConventionSet.FindMostSimilarConvention(multipleFrenchQuotesTabulator),
            Is.EqualTo((standardFrenchQuoteConvention, 1.0))
        );

        var noisyMultipleEnglishQuotesTabulator = new QuotationMarkTabulator();
        noisyMultipleEnglishQuotesTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u201c",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    5,
                    6
                ),
                new QuotationMarkMetadata(
                    "\u2019",
                    2,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    13,
                    14
                ),
                new QuotationMarkMetadata(
                    "\u201d",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    14,
                    15
                ),
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    28,
                    29
                ),
                new QuotationMarkMetadata(
                    "\u201d",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    42,
                    43
                ),
            ]
        );
        (QuoteConvention convention, double similarity) = allThreeQuoteConventionSet.FindMostSimilarConvention(
            noisyMultipleEnglishQuotesTabulator
        );
        Assert.That(convention, Is.EqualTo(standardEnglishQuoteConvention));
        Assert.That(similarity, Is.EqualTo(0.9).Within(1e-9));
        (convention, similarity) = twoFrenchQuoteConventionSet.FindMostSimilarConvention(
            noisyMultipleEnglishQuotesTabulator
        );
        Assert.That(convention, Is.EqualTo(westernEuropeanQuoteConvention));
        Assert.That(similarity, Is.EqualTo(0.1).Within(1e-9));

        var noisyMultipleFrenchQuotesTabulator = new QuotationMarkTabulator();
        noisyMultipleFrenchQuotesTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u00ab",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u2039",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    5,
                    6
                ),
                new QuotationMarkMetadata(
                    "\u203a",
                    2,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    13,
                    14
                ),
                new QuotationMarkMetadata(
                    "\u2039",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    5,
                    6
                ),
                new QuotationMarkMetadata(
                    "\u2019",
                    2,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    13,
                    14
                ),
                new QuotationMarkMetadata(
                    "\u00bb",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    14,
                    15
                ),
                new QuotationMarkMetadata(
                    "\u00ab",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    28,
                    29
                ),
                new QuotationMarkMetadata(
                    "\u00bb",
                    1,
                    QuotationMarkDirection.Closing,
                    new TextSegment.Builder().Build(),
                    42,
                    43
                ),
            ]
        );
        (convention, similarity) = allThreeQuoteConventionSet.FindMostSimilarConvention(
            noisyMultipleFrenchQuotesTabulator
        );
        Assert.That(convention, Is.EqualTo(standardFrenchQuoteConvention));
        Assert.That(similarity, Is.EqualTo(0.916666666666).Within(1e-9));

        var tooDeepEnglishQuotesTabulator = new QuotationMarkTabulator();
        tooDeepEnglishQuotesTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                ),
                new QuotationMarkMetadata(
                    "\u2018",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    5,
                    6
                ),
                new QuotationMarkMetadata(
                    "\u201c",
                    3,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    13,
                    14
                ),
                new QuotationMarkMetadata(
                    "\u2018",
                    4,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    15,
                    16
                ),
                new QuotationMarkMetadata(
                    "\u201c",
                    5,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    17,
                    18
                ),
            ]
        );
        (convention, similarity) = allThreeQuoteConventionSet.FindMostSimilarConvention(tooDeepEnglishQuotesTabulator);
        Assert.That(convention, Is.EqualTo(standardEnglishQuoteConvention));
        Assert.That(similarity, Is.EqualTo(0.967741935483871).Within(1e-9));

        // in case of ties, the earlier convention in the list should be returned
        var unknownQuoteTabulator = new QuotationMarkTabulator();
        unknownQuoteTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u201a",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                )
            ]
        );
        Assert.That(
            allThreeQuoteConventionSet.FindMostSimilarConvention(unknownQuoteTabulator),
            Is.EqualTo((standardEnglishQuoteConvention, 0.0))
        );

        var singleFrenchOpeningQuoteTabulator = new QuotationMarkTabulator();
        singleFrenchOpeningQuoteTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u00ab",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                )
            ]
        );
        Assert.That(
            allThreeQuoteConventionSet.FindMostSimilarConvention(singleFrenchOpeningQuoteTabulator),
            Is.EqualTo((standardFrenchQuoteConvention, 1.0))
        );
        Assert.That(
            twoFrenchQuoteConventionSet.FindMostSimilarConvention(singleFrenchOpeningQuoteTabulator),
            Is.EqualTo((westernEuropeanQuoteConvention, 1.0))
        );

        // Default values should be returned when the QuoteConventionSet is empty
        var singleEnglishOpeningQuoteTabulator = new QuotationMarkTabulator();
        singleEnglishOpeningQuoteTabulator.Tabulate(
            [
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().Build(),
                    0,
                    1
                )
            ]
        );
        var emptyQuoteConventionSet = new QuoteConventionSet([]);
        Assert.That(
            emptyQuoteConventionSet.FindMostSimilarConvention(singleEnglishOpeningQuoteTabulator),
            Is.EqualTo(((QuoteConvention?)null, double.MinValue))
        );
    }

    private class QuotationMarkPairMapEqualityComparer : IEqualityComparer<KeyValuePair<string, HashSet<string>>>
    {
        public bool Equals(KeyValuePair<string, HashSet<string>> x, KeyValuePair<string, HashSet<string>> y)
        {
            return x.Key == y.Key && x.Value.Count == y.Value.Count && !x.Value.Except(y.Value).Any();
        }

        public int GetHashCode([DisallowNull] KeyValuePair<string, HashSet<string>> obj)
        {
            return obj.GetHashCode();
        }
    }
}
