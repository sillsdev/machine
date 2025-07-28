using NUnit.Framework;
using SIL.Machine.Corpora.PunctuationAnalysis;

namespace SIL.Machine.Corpora;

[TestFixture]
public class QuotationMarkUpdateFirstPassTests
{
    [Test]
    public void CheckWhetherFallbackModeWillWork()
    {
        var firstPassAnalyzer = new QuotationMarkUpdateFirstPass(
            new QuoteConvention("", []),
            new QuoteConvention("", [])
        );

        // Cases where we expect fallback mode to work
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_english"),
                GetQuoteConventionByName("standard_english")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_french"),
                GetQuoteConventionByName("british_english")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("typewriter_western_european"),
                GetQuoteConventionByName("standard_russian")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("typewriter_western_european_variant"),
                GetQuoteConventionByName("standard_arabic")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("central_european"),
                GetQuoteConventionByName("british_typewriter_english")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_swedish"),
                GetQuoteConventionByName("typewriter_french")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_finnish"),
                GetQuoteConventionByName("british_inspired_western_european")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("eastern_european"),
                GetQuoteConventionByName("central_european")
            )
        );

        // Cases where we expect fallback mode to fail
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_english"),
                GetQuoteConventionByName("western_european")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("typewriter_french"),
                GetQuoteConventionByName("western_european")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_french"),
                GetQuoteConventionByName("french_variant")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("central_european"),
                GetQuoteConventionByName("typewriter_western_european")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("eastern_european"),
                GetQuoteConventionByName("standard_russian")
            )
        );
    }

    [Test]
    public void CheckWhetherFallbackModeWillWorkWithNormalizedConventions()
    {
        var firstPassAnalyzer = new QuotationMarkUpdateFirstPass(
            new QuoteConvention("", []),
            new QuoteConvention("", [])
        );

        // Cases where we expect fallback mode to work
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_english").Normalize(),
                GetQuoteConventionByName("standard_english")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_french").Normalize(),
                GetQuoteConventionByName("british_english")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("typewriter_western_european").Normalize(),
                GetQuoteConventionByName("standard_russian")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("typewriter_western_european_variant").Normalize(),
                GetQuoteConventionByName("standard_arabic")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("central_european").Normalize(),
                GetQuoteConventionByName("british_typewriter_english")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_swedish").Normalize(),
                GetQuoteConventionByName("typewriter_french")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_finnish").Normalize(),
                GetQuoteConventionByName("british_inspired_western_european")
            )
        );
        Assert.IsTrue(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("eastern_european").Normalize(),
                GetQuoteConventionByName("central_european")
            )
        );

        // Cases where we expect fallback mode to fail
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("western_european").Normalize(),
                GetQuoteConventionByName("standard_english")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("french_variant").Normalize(),
                GetQuoteConventionByName("hybrid_typewriter_english")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("british_inspired_western_european").Normalize(),
                GetQuoteConventionByName("standard_russian")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("typewriter_english").Normalize(),
                GetQuoteConventionByName("western_european")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("central_european_guillemets").Normalize(),
                GetQuoteConventionByName("french_variant")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_arabic").Normalize(),
                GetQuoteConventionByName("hybrid_typewriter_english")
            )
        );
        Assert.IsFalse(
            firstPassAnalyzer.CheckWhetherFallbackModeWillWork(
                GetQuoteConventionByName("standard_russian").Normalize(),
                GetQuoteConventionByName("standard_french")
            )
        );
    }

    [Test]
    public void ChooseBestActionForChapter()
    {
        // Verse text with no issues
        var actualAction = RunFirstPassOnChapter(
            [
                "Now the serpent was more subtle than any animal "
                    + "of the field which Yahweh God had made. "
                    + "He said to the woman, “Has God really said, "
                    + "‘You shall not eat of any tree of the garden’?”"
            ],
            "standard_english",
            "standard_english"
        );
        var expectedAction = QuotationMarkUpdateStrategy.ApplyFull;
        Assert.That(actualAction, Is.EqualTo(expectedAction));

        // Verse text with unpaired opening quotation mark
        actualAction = RunFirstPassOnChapter(
            [
                "Now the serpent was more subtle than any animal "
                    + "of the field which Yahweh God had made. "
                    + "He said to the woman, “Has God really said, "
                    + "‘You shall not eat of any tree of the garden’?"
            ],
            "standard_english",
            "standard_english"
        );
        expectedAction = QuotationMarkUpdateStrategy.ApplyFallback;
        Assert.That(actualAction, Is.EqualTo(expectedAction));

        // Verse text with unpaired closing quotation mark
        actualAction = RunFirstPassOnChapter(
            [
                "Now the serpent was more subtle than any animal "
                    + "of the field which Yahweh God had made. "
                    + "He said to the woman, Has God really said, "
                    + "You shall not eat of any tree of the garden?”"
            ],
            "standard_english",
            "standard_english"
        );
        expectedAction = QuotationMarkUpdateStrategy.ApplyFallback;
        Assert.That(actualAction, Is.EqualTo(expectedAction));

        // Verse text with too deeply nested quotation marks
        actualAction = RunFirstPassOnChapter(
            [
                "“Now the serpent was more “subtle than any animal "
                    + "of the “field which “Yahweh God had made. "
                    + "He said to the woman, “Has God really said, "
                    + "“You shall not eat of any tree of the garden?"
            ],
            "standard_english",
            "standard_english"
        );
        expectedAction = QuotationMarkUpdateStrategy.ApplyFallback;
        Assert.That(actualAction, Is.EqualTo(expectedAction));

        // Verse text with an ambiguous quotation mark
        actualAction = RunFirstPassOnChapter(
            [
                "Now the serpent was more subtle than any animal "
                    + "of the field which Yahweh God had made. "
                    + "He said to the woman\"Has God really said, "
                    + "You shall not eat of any tree of the garden?"
            ],
            "typewriter_english",
            "standard_english"
        );
        expectedAction = QuotationMarkUpdateStrategy.Skip;
        Assert.That(actualAction, Is.EqualTo(expectedAction));

        // Verse text with an ambiguous quotation mark
        actualAction = RunFirstPassOnChapter(
            [
                "Now the serpent was more subtle than any animal "
                    + "of the field which Yahweh God had made. "
                    + "He said to the woman\"Has God really said, "
                    + "You shall not eat of any tree of the garden?"
            ],
            "typewriter_english",
            "standard_english"
        );
        expectedAction = QuotationMarkUpdateStrategy.Skip;
        Assert.That(actualAction, Is.EqualTo(expectedAction));

        // Verse text with too deeply nested ambiguous quotation marks
        actualAction = RunFirstPassOnChapter(
            [
                "\"Now the serpent was more \"subtle than any animal "
                    + "of the \"field which \"Yahweh God had made. "
                    + "He said to the woman, \"Has God really said, "
                    + "\"You shall not eat of any tree of the garden?"
            ],
            "typewriter_english",
            "standard_english"
        );
        expectedAction = QuotationMarkUpdateStrategy.Skip;
        Assert.That(actualAction, Is.EqualTo(expectedAction));
    }

    [Test]
    public void ChooseBestActionBasedOnObservedIssues()
    {
        var firstPassAnalyzer = new QuotationMarkUpdateFirstPass(
            new QuoteConvention("", []),
            new QuoteConvention("", [])
        );
        firstPassAnalyzer.WillFallbackModeWork = false;

        // Test with no issue
        var bestAction = firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues([]);
        Assert.That(bestAction, Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFull));

        // Test with one issue
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [QuotationMarkResolutionIssue.UnpairedQuotationMark]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [QuotationMarkResolutionIssue.AmbiguousQuotationMark]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues([QuotationMarkResolutionIssue.TooDeepNesting]),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );

        // Test with multiple issues
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [QuotationMarkResolutionIssue.TooDeepNesting, QuotationMarkResolutionIssue.AmbiguousQuotationMark,]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [
                    QuotationMarkResolutionIssue.UnpairedQuotationMark,
                    QuotationMarkResolutionIssue.AmbiguousQuotationMark,
                ]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [QuotationMarkResolutionIssue.TooDeepNesting, QuotationMarkResolutionIssue.UnpairedQuotationMark,]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );
    }

    [Test]
    public void ChooseBestActionBasedOnObservedIssuesWithBasicFallback()
    {
        var firstPassAnalyzer = new QuotationMarkUpdateFirstPass(
            new QuoteConvention("", []),
            new QuoteConvention("", [])
        );
        firstPassAnalyzer.WillFallbackModeWork = true;

        // Test with no issues
        var bestAction = firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues([]);
        Assert.That(bestAction, Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFull));

        // Test with one issue
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [QuotationMarkResolutionIssue.UnpairedQuotationMark]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFallback)
        );
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [QuotationMarkResolutionIssue.AmbiguousQuotationMark]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues([QuotationMarkResolutionIssue.TooDeepNesting]),
            Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFallback)
        );

        // Test with multiple issues
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [
                    QuotationMarkResolutionIssue.AmbiguousQuotationMark,
                    QuotationMarkResolutionIssue.UnpairedQuotationMark,
                ]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [QuotationMarkResolutionIssue.AmbiguousQuotationMark, QuotationMarkResolutionIssue.TooDeepNesting,]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.Skip)
        );
        Assert.That(
            firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues(
                [QuotationMarkResolutionIssue.TooDeepNesting, QuotationMarkResolutionIssue.UnpairedQuotationMark,]
            ),
            Is.EqualTo(QuotationMarkUpdateStrategy.ApplyFallback)
        );

        // tests of getBestActionsByChapter()
    }

    [Test]
    public void NoIssuesInUsfm()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?”
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.ApplyFull];
        var observedActions = RunFirstPass(normalizedUsfm, "standard_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedOpeningMark()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.ApplyFallback];
        var observedActions = RunFirstPass(normalizedUsfm, "standard_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedClosingMark()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, Has God really said,
    You shall not eat of any tree of the garden?”
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.ApplyFallback];
        var observedActions = RunFirstPass(normalizedUsfm, "standard_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void TooDeepNesting()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 “Now the serpent was more “subtle than any animal
    of the “field which “Yahweh God had made.
    He said to the woman, “Has God really said,
    “You shall not eat of any tree of the garden?
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.ApplyFallback];
        var observedActions = RunFirstPass(normalizedUsfm, "standard_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousQuotationMark()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman""Has God really said,
    You shall not eat of any tree of the garden?
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.Skip];
        var observedActions = RunFirstPass(normalizedUsfm, "typewriter_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void NoIssuesInMultipleChapters()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    \c 2 \v 1 He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?”
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.ApplyFull,
            QuotationMarkUpdateStrategy.ApplyFull
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "standard_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedQuotationMarkInSecondChapter()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    \c 2 \v 1 He said to the woman, Has God really said,
    You shall not eat of any tree of the garden?”
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.ApplyFull,
            QuotationMarkUpdateStrategy.ApplyFallback
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "standard_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedQuotationMarkInFirstChapter()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had” made.
    \c 2 \v 1 He said to the woman, Has God really said,
    “You shall not eat of any tree of the garden?”
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.ApplyFallback,
            QuotationMarkUpdateStrategy.ApplyFull
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "standard_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousQuotationMarkInSecondChapter()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    \c 2 \v 1 He said to the woman, Has God really said,
    You shall not""eat of any tree of the garden?""
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.ApplyFull,
            QuotationMarkUpdateStrategy.Skip
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "typewriter_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousQuotationMarkInFirstChapter()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field""which Yahweh God had made.
    \c 2 \v 1 He said to the woman, Has God really said,
    ""You shall not eat of any tree of the garden?""
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.Skip,
            QuotationMarkUpdateStrategy.ApplyFull
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "typewriter_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedQuotationMarkInBothChapters()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had” made.
    \c 2 \v 1 He said to the woman, Has God really said,
    You shall not eat of any tree of the garden?”
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.ApplyFallback,
            QuotationMarkUpdateStrategy.ApplyFallback
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "standard_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousQuotationMarkInBothChapters()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had""made.
    \c 2 \v 1 He said to the woman, Has God really said,
    You shall not eat of any""tree of the garden?
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.Skip,
            QuotationMarkUpdateStrategy.Skip
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "typewriter_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedInFirstAmbiguousInSecond()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.""
    \c 2 \v 1 He said to the woman, Has God really said,
    You shall not eat of any""tree of the garden?
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.ApplyFallback,
            QuotationMarkUpdateStrategy.Skip
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "typewriter_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousInFirstUnpairedInSecond()
    {
        var normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God""had made.
    \c 2 \v 1 He said to the woman, Has God really said,
    You shall not eat of any tree of the garden ? ""
    ";
        List<QuotationMarkUpdateStrategy> expectedActions =
        [
            QuotationMarkUpdateStrategy.Skip,
            QuotationMarkUpdateStrategy.ApplyFallback
        ];
        var observedActions = RunFirstPass(normalizedUsfm, "typewriter_english", "standard_english");

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    public List<QuotationMarkUpdateStrategy> RunFirstPass(
        string normalizedUsfm,
        string sourceQuoteConventionName,
        string targetQuoteConventionName
    )
    {
        var sourceQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            sourceQuoteConventionName
        );
        Assert.IsNotNull(sourceQuoteConvention);

        var targetQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            targetQuoteConventionName
        );
        Assert.IsNotNull(targetQuoteConvention);

        var firstPassAnalyzer = new QuotationMarkUpdateFirstPass(sourceQuoteConvention, targetQuoteConvention);
        UsfmParser.Parse(normalizedUsfm, firstPassAnalyzer);

        return firstPassAnalyzer.FindBestChapterStrategies();
    }

    public QuotationMarkUpdateStrategy RunFirstPassOnChapter(
        List<string> verseTexts,
        string sourceQuoteConventionName,
        string targetQuoteConventionName
    )
    {
        var sourceQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            sourceQuoteConventionName
        );
        Assert.IsNotNull(sourceQuoteConvention);

        var targetQuoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(
            targetQuoteConventionName
        );
        Assert.IsNotNull(targetQuoteConvention);

        var firstPassAnalyzer = new QuotationMarkUpdateFirstPass(sourceQuoteConvention, targetQuoteConvention);

        var chapter = new Chapter(
            verseTexts.Select(verseText => new Verse([new TextSegment.Builder().SetText(verseText).Build()])).ToList()
        );

        return firstPassAnalyzer.FindBestStrategyForChapter(chapter);
    }

    public QuoteConvention GetQuoteConventionByName(string name)
    {
        var quoteConvention = StandardQuoteConventions.QuoteConventions.GetQuoteConventionByName(name);
        Assert.IsNotNull(quoteConvention);
        return quoteConvention;
    }
}
