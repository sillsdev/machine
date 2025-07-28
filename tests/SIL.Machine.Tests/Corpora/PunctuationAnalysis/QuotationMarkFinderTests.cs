using NUnit.Framework;

namespace SIL.Machine.Corpora.PunctuationAnalysis;

[TestFixture]
public class QuotationMarkFinderTests
{
    [Test]
    public void ThatAllPossibleQuotationMarksAreIdentified()
    {
        var quotationMarkFinder = new QuotationMarkFinder(StandardQuoteConventions.QuoteConventions);
        Assert.That(
            quotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder().SetText("\u201cSample Text\u201d").Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder().SetText("\u201cSample Text\u201d").Build(),
                            0,
                            1
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder().SetText("\u201cSample Text\u201d").Build(),
                            12,
                            13
                        ),
                    ]
                )
        );

        Assert.That(
            quotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder().SetText("\"Sample Text'").Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"Sample Text'").Build(), 0, 1),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder().SetText("\"Sample Text'").Build(),
                            12,
                            13
                        ),
                    ]
                )
        );

        Assert.That(
            quotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder().SetText("All \u201cthe \u2019English quotation\u2018 marks\u201d").Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u201cthe \u2019English quotation\u2018 marks\u201d")
                                .Build(),
                            4,
                            5
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u201cthe \u2019English quotation\u2018 marks\u201d")
                                .Build(),
                            9,
                            10
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u201cthe \u2019English quotation\u2018 marks\u201d")
                                .Build(),
                            27,
                            28
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u201cthe \u2019English quotation\u2018 marks\u201d")
                                .Build(),
                            34,
                            35
                        )
                    ]
                )
        );

        Assert.That(
            quotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder().SetText("All \u00abthe \u2039French quotation\u203a marks\u00bb").Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u2039French quotation\u203a marks\u00bb")
                                .Build(),
                            4,
                            5
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u2039French quotation\u203a marks\u00bb")
                                .Build(),
                            9,
                            10
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u2039French quotation\u203a marks\u00bb")
                                .Build(),
                            26,
                            27
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u2039French quotation\u203a marks\u00bb")
                                .Build(),
                            33,
                            34
                        ),
                    ]
                )
        );

        Assert.That(
            quotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder().SetText("All \"the 'typewriter quotation marks").Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder().SetText("All \"the 'typewriter quotation marks").Build(),
                            4,
                            5
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder().SetText("All \"the 'typewriter quotation marks").Build(),
                            9,
                            10
                        ),
                    ]
                )
        );

        Assert.That(
            quotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder()
                        .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                        .Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            9,
                            10
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            22,
                            23
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            45,
                            47
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            53,
                            54
                        ),
                    ]
                )
        );

        Assert.That(
            quotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder()
                        .SetText("All \u00abthe \u201cWestern \u2018european\u2019 quotation\u201d marks\u00bb")
                        .Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u201cWestern \u2018european\u2019 quotation\u201d marks\u00bb")
                                .Build(),
                            4,
                            5
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u201cWestern \u2018european\u2019 quotation\u201d marks\u00bb")
                                .Build(),
                            9,
                            10
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u201cWestern \u2018european\u2019 quotation\u201d marks\u00bb")
                                .Build(),
                            18,
                            19
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u201cWestern \u2018european\u2019 quotation\u201d marks\u00bb")
                                .Build(),
                            27,
                            28
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u201cWestern \u2018european\u2019 quotation\u201d marks\u00bb")
                                .Build(),
                            38,
                            39
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u00abthe \u201cWestern \u2018european\u2019 quotation\u201d marks\u00bb")
                                .Build(),
                            45,
                            46
                        ),
                    ]
                )
        );

        Assert.That(
            quotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder()
                        .SetText("All \u201ethe \u201aCentral European quotation\u2018 marks\u201c")
                        .Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u201ethe \u201aCentral European quotation\u2018 marks\u201c")
                                .Build(),
                            4,
                            5
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u201ethe \u201aCentral European quotation\u2018 marks\u201c")
                                .Build(),
                            9,
                            10
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u201ethe \u201aCentral European quotation\u2018 marks\u201c")
                                .Build(),
                            36,
                            37
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("All \u201ethe \u201aCentral European quotation\u2018 marks\u201c")
                                .Build(),
                            43,
                            44
                        ),
                    ]
                )
        );
    }

    [Test]
    public void ThatItUsesTheQuoteConventionSet()
    {
        var standardEnglishQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            "standard_english"
        );
        Assert.IsNotNull(standardEnglishQuoteConvention);

        var englishQuotationMarkFinder = new QuotationMarkFinder(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        Assert.That(
            englishQuotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder()
                        .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                        .Build()
                )
                .ToList(),
            Has.Count.EqualTo(0)
        );

        var typewriterEnglishQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            "typewriter_english"
        );
        Assert.IsNotNull(typewriterEnglishQuoteConvention);

        var typewriterEnglishQuotationMarkFinder = new QuotationMarkFinder(
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        Assert.That(
            typewriterEnglishQuotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder()
                        .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                        .Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            53,
                            54
                        )
                    ]
                )
        );

        var westernEuropeanQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            "western_european"
        );
        Assert.IsNotNull(westernEuropeanQuoteConvention);

        var westernEuropeanQuotationMarkFinder = new QuotationMarkFinder(
            new QuoteConventionSet([westernEuropeanQuoteConvention])
        );
        Assert.IsTrue(
            westernEuropeanQuotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder()
                        .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                        .Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            22,
                            23
                        )
                    ]
                )
        );

        var typewriterWesternEuropeanQuoteConvention =
            StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName("typewriter_western_european");
        Assert.IsNotNull(typewriterWesternEuropeanQuoteConvention);

        var typewriterWesternEuropeanQuotationMarkFinder = new QuotationMarkFinder(
            new QuoteConventionSet([typewriterWesternEuropeanQuoteConvention])
        );
        Assert.That(
            typewriterWesternEuropeanQuotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder()
                        .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                        .Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            45,
                            47
                        ),
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            53,
                            54
                        ),
                    ]
                )
        );

        var centralEuropeanQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            "central_european"
        );
        Assert.IsNotNull(centralEuropeanQuoteConvention);

        var centralEuropeanQuotationMarkFinder = new QuotationMarkFinder(
            new QuoteConventionSet([centralEuropeanQuoteConvention])
        );
        Assert.IsTrue(
            centralEuropeanQuotationMarkFinder
                .FindAllPotentialQuotationMarksInTextSegment(
                    new TextSegment.Builder()
                        .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                        .Build()
                )
                .SequenceEqual(
                    [
                        new QuotationMarkStringMatch(
                            new TextSegment.Builder()
                                .SetText("This has \u201equotes from \u00bbdifferent conventions <<mixed 'together")
                                .Build(),
                            9,
                            10
                        )
                    ]
                )
        );
    }
}
