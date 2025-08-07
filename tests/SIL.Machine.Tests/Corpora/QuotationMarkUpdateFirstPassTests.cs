using NUnit.Framework;
using SIL.Machine.PunctuationAnalysis;

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
        QuotationMarkUpdateStrategy actualAction = RunFirstPassOnChapter(
            [
                "Now the serpent was more subtle than any animal "
                    + "of the field which Yahweh God had made. "
                    + "He said to the woman, “Has God really said, "
                    + "‘You shall not eat of any tree of the garden’?”"
            ],
            "standard_english",
            "standard_english"
        );
        QuotationMarkUpdateStrategy expectedAction = QuotationMarkUpdateStrategy.ApplyFull;
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
        QuotationMarkUpdateStrategy bestAction = firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues([]);
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
        QuotationMarkUpdateStrategy bestAction = firstPassAnalyzer.ChooseBestStrategyBasedOnObservedIssues([]);
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
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?”
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.ApplyFull];
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "standard_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedOpeningMark()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, “Has God really said,
    ‘You shall not eat of any tree of the garden’?
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.ApplyFallback];
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "standard_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedClosingMark()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman, Has God really said,
    You shall not eat of any tree of the garden?”
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.ApplyFallback];
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "standard_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void TooDeepNesting()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 “Now the serpent was more “subtle than any animal
    of the “field which “Yahweh God had made.
    He said to the woman, “Has God really said,
    “You shall not eat of any tree of the garden?
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.ApplyFallback];
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "standard_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousQuotationMark()
    {
        string normalizedUsfm =
            @"\c 1
    \v 1 Now the serpent was more subtle than any animal
    of the field which Yahweh God had made.
    He said to the woman""Has God really said,
    You shall not eat of any tree of the garden?
    ";
        List<QuotationMarkUpdateStrategy> expectedActions = [QuotationMarkUpdateStrategy.Skip];
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "typewriter_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void NoIssuesInMultipleChapters()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "standard_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedQuotationMarkInSecondChapter()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "standard_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedQuotationMarkInFirstChapter()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "standard_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousQuotationMarkInSecondChapter()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "typewriter_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousQuotationMarkInFirstChapter()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "typewriter_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedQuotationMarkInBothChapters()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "standard_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousQuotationMarkInBothChapters()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "typewriter_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void UnpairedInFirstAmbiguousInSecond()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "typewriter_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    [Test]
    public void AmbiguousInFirstUnpairedInSecond()
    {
        string normalizedUsfm =
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
        List<QuotationMarkUpdateStrategy> observedActions = RunFirstPass(
            normalizedUsfm,
            "typewriter_english",
            "standard_english"
        );

        Assert.That(expectedActions.SequenceEqual(observedActions));
    }

    public List<QuotationMarkUpdateStrategy> RunFirstPass(
        string normalizedUsfm,
        string sourceQuoteConventionName,
        string targetQuoteConventionName
    )
    {
        QuoteConvention sourceQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
            sourceQuoteConventionName
        );
        Assert.IsNotNull(sourceQuoteConvention);

        QuoteConvention targetQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
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
        QuoteConvention sourceQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
            sourceQuoteConventionName
        );
        Assert.IsNotNull(sourceQuoteConvention);

        QuoteConvention targetQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
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
        QuoteConvention quoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(name);
        Assert.IsNotNull(quoteConvention);
        return quoteConvention;
    }
}
