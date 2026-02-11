using NUnit.Framework;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class DepthBasedQuotationMarkResolverTests
{
    [Test]
    public void CurrentDepthQuotationMarkResolverState()
    {
        var state = new QuotationMarkResolverState();
        Assert.That(state.CurrentDepth, Is.EqualTo(0));

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(state.CurrentDepth, Is.EqualTo(1));

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(state.CurrentDepth, Is.EqualTo(2));

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
        );
        Assert.That(state.CurrentDepth, Is.EqualTo(1));

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
        );
        Assert.That(state.CurrentDepth, Is.EqualTo(0));
    }

    [Test]
    public void HasOpenQuotationMark()
    {
        var state = new QuotationMarkResolverState();
        Assert.That(state.HasOpenQuotationMark, Is.False);

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(state.HasOpenQuotationMark, Is.True);

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(state.HasOpenQuotationMark, Is.True);

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
        );
        Assert.That(state.HasOpenQuotationMark, Is.True);

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
        );
        Assert.That(state.HasOpenQuotationMark, Is.False);
    }

    [Test]
    public void AreMoreThanNQuotesOpen()
    {
        var state = new QuotationMarkResolverState();
        Assert.That(state.AreMoreThanNQuotesOpen(1), Is.False);
        Assert.That(state.AreMoreThanNQuotesOpen(2), Is.False);

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(state.AreMoreThanNQuotesOpen(1), Is.False);
        Assert.That(state.AreMoreThanNQuotesOpen(2), Is.False);

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(state.AreMoreThanNQuotesOpen(1), Is.True);
        Assert.That(state.AreMoreThanNQuotesOpen(2), Is.False);

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
        );
        Assert.That(state.AreMoreThanNQuotesOpen(1), Is.False);
        Assert.That(state.AreMoreThanNQuotesOpen(2), Is.False);

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
        );
        Assert.That(state.AreMoreThanNQuotesOpen(1), Is.False);
        Assert.That(state.AreMoreThanNQuotesOpen(2), Is.False);
    }

    [Test]
    public void GetOpeningQuotationMarkAtDepth()
    {
        var state = new QuotationMarkResolverState();
        Assert.Throws<InvalidOperationException>(() => state.GetOpeningQuotationMarkAtDepth(1));

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(state.GetOpeningQuotationMarkAtDepth(1), Is.EqualTo("\u201c"));
        Assert.Throws<InvalidOperationException>(() => state.GetOpeningQuotationMarkAtDepth(2));

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(state.GetOpeningQuotationMarkAtDepth(1), Is.EqualTo("\u201c"));
        Assert.That(state.GetOpeningQuotationMarkAtDepth(2), Is.EqualTo("\u2018"));

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
        );
        Assert.That(state.GetOpeningQuotationMarkAtDepth(1), Is.EqualTo("\u201c"));
        Assert.Throws<InvalidOperationException>(() => state.GetOpeningQuotationMarkAtDepth(2));

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
        );
        Assert.Throws<InvalidOperationException>(() => state.GetOpeningQuotationMarkAtDepth(1));
    }

    [Test]
    public void GetDeepestOpeningMark()
    {
        var state = new QuotationMarkResolverState();
        Assert.Throws<InvalidOperationException>(() => state.GetDeepestOpeningQuotationMark());

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(state.GetDeepestOpeningQuotationMark(), Is.EqualTo("\u201c"));

        state.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(state.GetDeepestOpeningQuotationMark(), Is.EqualTo("\u2018"));

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
        );
        Assert.That(state.GetDeepestOpeningQuotationMark(), Is.EqualTo("\u201c"));

        state.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
        );
        Assert.Throws<InvalidOperationException>(() => state.GetDeepestOpeningQuotationMark());
    }

    [Test]
    public void GetCurrentDepthQuotationContinuerState()
    {
        var resolverState = new QuotationMarkResolverState();
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );

        var continuerState = new TestQuoteContinuerState();
        Assert.That(continuerState.CurrentDepth, Is.EqualTo(0));

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(continuerState.CurrentDepth, Is.EqualTo(1));

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(continuerState.CurrentDepth, Is.EqualTo(2));

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(continuerState.CurrentDepth, Is.EqualTo(0));
    }

    [Test]
    public void HasContinuerBeenObserved()
    {
        var resolverState = new QuotationMarkResolverState();
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );

        var continuerState = new TestQuoteContinuerState();
        Assert.That(continuerState.ContinuerHasBeenObserved(), Is.False);

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(continuerState.ContinuerHasBeenObserved(), Is.True);

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(continuerState.ContinuerHasBeenObserved(), Is.True);

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(continuerState.ContinuerHasBeenObserved(), Is.False);
    }

    [Test]
    public void GetContinuerStyle()
    {
        var resolverState = new QuotationMarkResolverState();
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );

        var continuerState = new TestQuoteContinuerState();
        Assert.That(continuerState.InternalContinuerStyle, Is.EqualTo(QuoteContinuerStyle.Undetermined));

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(continuerState.InternalContinuerStyle, Is.EqualTo(QuoteContinuerStyle.English));

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.Spanish
        );
        Assert.That(continuerState.InternalContinuerStyle, Is.EqualTo(QuoteContinuerStyle.Spanish));

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(continuerState.InternalContinuerStyle, Is.EqualTo(QuoteContinuerStyle.English));
    }

    [Test]
    public void AddQuotationContinuer()
    {
        var resolverState = new QuotationMarkResolverState();
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );

        var continuerState = new TestQuoteContinuerState();

        QuotationMarkMetadata result1 = continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(
            result1,
            Is.EqualTo(
                new QuotationMarkMetadata(
                    "\u201c",
                    1,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().SetText("\u201c").Build(),
                    0,
                    1
                )
            )
        );

        QuotationMarkMetadata result2 = continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.Spanish
        );
        Assert.That(
            result2,
            Is.EqualTo(
                new QuotationMarkMetadata(
                    "\u2018",
                    2,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().SetText("\u2018").Build(),
                    0,
                    1
                )
            )
        );
        Assert.That(continuerState.InternalContinuerStyle, Is.EqualTo(QuoteContinuerStyle.Spanish));

        QuotationMarkMetadata result3 = continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1),
            resolverState,
            QuoteContinuerStyle.English
        );
        Assert.That(
            result3,
            Is.EqualTo(
                new QuotationMarkMetadata(
                    "\u201c",
                    3,
                    QuotationMarkDirection.Opening,
                    new TextSegment.Builder().SetText("\u201c").Build(),
                    0,
                    1
                )
            )
        );
    }

    [Test]
    public void IsEnglishQuotationContinuer()
    {
        QuoteConvention standardEnglish = QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
        Assert.That(standardEnglish, Is.Not.Null);

        var settings = new QuoteConventionDetectionResolutionSettings(new QuoteConventionSet([standardEnglish]));
        var resolverState = new QuotationMarkResolverState();
        var continuerState = new TestQuoteContinuerState();
        var categorizer = new QuotationMarkCategorizer(settings, resolverState, continuerState);

        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );

        // Should always be false if the continuer style is Spanish
        continuerState.InternalContinuerStyle = QuoteContinuerStyle.English;
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.True
        );

        continuerState.InternalContinuerStyle = QuoteContinuerStyle.Spanish;
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.False
        );
        continuerState.InternalContinuerStyle = QuoteContinuerStyle.English;

        // Should be false if there's no preceding paragraph marker (and the settings say to rely on markers)
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201ctest").Build(), 0, 1),
                null,
                null
            ),
            Is.False
        );

        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.True
        );

        var categorizerForDenorm = new QuotationMarkCategorizer(
            new QuotationMarkUpdateResolutionSettings(standardEnglish),
            resolverState,
            continuerState
        );
        Assert.That(
            categorizerForDenorm.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201ctest").Build(), 0, 1),
                null,
                null
            ),
            Is.True
        );

        // Should be false if there are no open quotation marks
        var emptyState = new QuotationMarkResolverState();
        var emptyCategorizer = new QuotationMarkCategorizer(settings, emptyState, continuerState);
        Assert.That(
            emptyCategorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.False
        );

        // Should be false if the starting index of the quotation mark is greater than 0
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText(" \u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                ),
                null,
                null
            ),
            Is.False
        );

        // Should be false if the mark does not match the already opened mark
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u2018test")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.False
        );

        // If there are multiple open quotes, the next quote continuer must follow immediately after the current one
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.False
        );
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u2018test")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u2018test")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                )
            ),
            Is.True
        );
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                )
            ),
            Is.True
        );

        // When there are multiple open quotes, the continuer must match the deepest observed mark
        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(
                new TextSegment.Builder()
                    .SetText("\u201c\u2018test")
                    .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                    .Build(),
                0,
                1
            ),
            resolverState,
            QuoteContinuerStyle.English
        );

        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                ),
                null,
                null
            ),
            Is.False
        );
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u2018test")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                ),
                null,
                null
            ),
            Is.True
        );

        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u2018\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                ),
                null,
                null
            ),
            Is.True
        );

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(
                new TextSegment.Builder()
                    .SetText("\u201c\u2018\u201ctest")
                    .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                    .Build(),
                1,
                2
            ),
            resolverState,
            QuoteContinuerStyle.English
        );

        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u2018\u2018test")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    2,
                    3
                ),
                null,
                null
            ),
            Is.False
        );
        Assert.That(
            categorizer.IsEnglishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201c\u2018\u201ctest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    2,
                    3
                ),
                null,
                null
            ),
            Is.True
        );
    }

    [Test]
    public void IsSpanishQuotationContinuer()
    {
        QuoteConvention westernEuropeanQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
            "western_european"
        );
        Assert.That(westernEuropeanQuoteConvention, Is.Not.Null);

        var settings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([westernEuropeanQuoteConvention])
        );
        var resolverState = new QuotationMarkResolverState();
        var continuerState = new TestQuoteContinuerState();
        var categorizer = new QuotationMarkCategorizer(settings, resolverState, continuerState);

        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00ab").Build(), 0, 1)
        );

        // Should always be false if the continuer style is Spanish
        continuerState.InternalContinuerStyle = QuoteContinuerStyle.Spanish;
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.True
        );

        continuerState.InternalContinuerStyle = QuoteContinuerStyle.English;
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.False
        );
        continuerState.InternalContinuerStyle = QuoteContinuerStyle.Spanish;

        // Should be false if there's no preceding paragraph marker (and the settings say to rely on markers)
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bbtest").Build(), 0, 1),
                null,
                null
            ),
            Is.False
        );

        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.True
        );

        var categorizerForDenorm = new QuotationMarkCategorizer(
            new QuotationMarkUpdateResolutionSettings(westernEuropeanQuoteConvention),
            resolverState,
            continuerState
        );
        Assert.That(
            categorizerForDenorm.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bbtest").Build(), 0, 1),
                null,
                null
            ),
            Is.True
        );

        // Should be false if there are no open quotation marks
        var emptyState = new QuotationMarkResolverState();
        var emptyCategorizer = new QuotationMarkCategorizer(settings, emptyState, continuerState);
        Assert.That(
            emptyCategorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.False
        );

        // Should be false if the starting index of the quotation mark is greater than 0
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText(" \u00bbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                ),
                null,
                null
            ),
            Is.False
        );

        // Should be false if the mark does not match the already opened mark
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u201dtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.False
        );

        // If there are multiple open quotes, the next quote continuer must follow immediately after the current one
        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                null
            ),
            Is.False
        );
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u201dtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u201dtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                )
            ),
            Is.True
        );
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u00bbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    0,
                    1
                ),
                null,
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u00bbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                )
            ),
            Is.True
        );

        // When there are multiple open quotes, the continuer must match the deepest observed mark
        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(
                new TextSegment.Builder()
                    .SetText("\u00bb\u201dtest")
                    .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                    .Build(),
                0,
                1
            ),
            resolverState,
            QuoteContinuerStyle.Spanish
        );

        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u201cbtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                ),
                null,
                null
            ),
            Is.False
        );
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u201dtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                ),
                null,
                null
            ),
            Is.True
        );

        resolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u201d\u2019test")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    1,
                    2
                ),
                null,
                null
            ),
            Is.True
        );

        continuerState.AddQuoteContinuer(
            new QuotationMarkStringMatch(
                new TextSegment.Builder()
                    .SetText("\u00bb\u201d\u2019test")
                    .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                    .Build(),
                1,
                2
            ),
            resolverState,
            QuoteContinuerStyle.Spanish
        );

        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u201d\u201dtest")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    2,
                    3
                ),
                null,
                null
            ),
            Is.False
        );
        Assert.That(
            categorizer.IsSpanishQuoteContinuer(
                new QuotationMarkStringMatch(
                    new TextSegment.Builder()
                        .SetText("\u00bb\u201d\u2019test")
                        .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                        .Build(),
                    2,
                    3
                ),
                null,
                null
            ),
            Is.True
        );
    }

    [Test]
    public void IsOpeningQuote()
    {
        QuoteConvention centralEuropeanQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("central_european")
        );
        Assert.That(centralEuropeanQuoteConvention, Is.Not.Null);
        var centralEuropeanResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([centralEuropeanQuoteConvention])
        );
        var quotationMarkResolverState = new QuotationMarkResolverState();
        var quotationContinuerState = new QuoteContinuerState();
        var centralEuropeanQuotationMarkCategorizer = new QuotationMarkCategorizer(
            centralEuropeanResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention britishEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("british_english")
        );
        Assert.That(britishEnglishQuoteConvention, Is.Not.Null);
        var britishEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([britishEnglishQuoteConvention])
        );
        var britishEnglishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            britishEnglishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention standardSwedishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_swedish")
        );
        Assert.That(standardSwedishQuoteConvention, Is.Not.Null);
        var standardSwedishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardSwedishQuoteConvention])
        );
        var standardSwedishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            standardSwedishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        var threeConventionsResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([
                centralEuropeanQuoteConvention,
                britishEnglishQuoteConvention,
                standardSwedishQuoteConvention,
            ])
        );
        var threeConventionsQuotationMarkCategorizer = new QuotationMarkCategorizer(
            threeConventionsResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        // It should only accept valid opening marks under the quote convention
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201e").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201a").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u00ab").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \"").Build(), 1, 2)
            ),
            Is.False
        );

        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201e").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201a").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u00ab").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \"").Build(), 1, 2)
            ),
            Is.False
        );

        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201e").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201a").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u00ab").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \"").Build(), 1, 2)
            ),
            Is.False
        );

        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201e").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201a").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u00ab").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \"").Build(), 1, 2)
            ),
            Is.False
        );

        // Leading whitespace is not necessary for unambiguous opening quotes
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("text\u201e").Build(), 4, 5)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("text\u201a").Build(), 4, 5)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("text\u201c").Build(), 4, 5)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("text\u2018").Build(), 4, 5)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("text\u201e").Build(), 4, 5)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("text\u201a").Build(), 4, 5)
            ),
            Is.True
        );

        // An ambiguous quotation mark (opening/closing) is recognized as opening if it has a quote introducer beforehand
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(",\u201d").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(":\u2019").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(",\u201c").Build(), 1, 2)
            ),
            Is.True
        );

        // An ambiguous quotation mark (opening/closing) is recognized as opening if preceded by another opening mark
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201d").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u2019").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u201c").Build(), 1, 2)
            ),
            Is.True
        );

        // An ambiguous quotation mark (opening/closing) is not recognized as opening if it has trailing whitespace or punctuation
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d.").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(",\u201d ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u2019 ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c\u2019?").Build(), 1, 2)
            ),
            Is.False
        );
    }

    [Test]
    public void IsClosingQuote()
    {
        QuoteConvention centralEuropeanQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("central_european")
        );
        Assert.That(centralEuropeanQuoteConvention, Is.Not.Null);
        var centralEuropeanResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([centralEuropeanQuoteConvention])
        );
        var quotationMarkResolverState = new QuotationMarkResolverState();
        var quotationContinuerState = new QuoteContinuerState();
        var centralEuropeanQuotationMarkCategorizer = new QuotationMarkCategorizer(
            centralEuropeanResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention britishEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("british_english")
        );
        Assert.That(britishEnglishQuoteConvention, Is.Not.Null);
        var britishEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([britishEnglishQuoteConvention])
        );
        var britishEnglishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            britishEnglishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention standardSwedishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_swedish")
        );
        Assert.That(standardSwedishQuoteConvention, Is.Not.Null);
        var standardSwedishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardSwedishQuoteConvention])
        );
        var standardSwedishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            standardSwedishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention standardFrenchQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_french")
        );
        Assert.That(standardFrenchQuoteConvention, Is.Not.Null);
        var standardFrenchResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardFrenchQuoteConvention])
        );
        var standardFrenchQuotationMarkCategorizer = new QuotationMarkCategorizer(
            standardFrenchResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        var threeConventionsResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([
                centralEuropeanQuoteConvention,
                britishEnglishQuoteConvention,
                standardSwedishQuoteConvention,
            ])
        );
        var threeConventionsQuotationMarkCategorizer = new QuotationMarkCategorizer(
            threeConventionsResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        // It should only accept valid closing marks under the quote convention
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018 ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201e ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201a ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019 ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\" ").Build(), 0, 1)
            ),
            Is.False
        );

        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018 ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019 ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\" ").Build(), 0, 1)
            ),
            Is.False
        );

        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018 ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019 ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\" ").Build(), 0, 1)
            ),
            Is.False
        );

        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018 ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019 ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\" ").Build(), 0, 1)
            ),
            Is.False
        );

        // Trailing whitespace is not necessary for unambiguous closing quotes
        Assert.That(
            standardFrenchQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bbtext").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardFrenchQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u203atext").Build(), 0, 1)
            ),
            Is.True
        );

        // An ambiguous quotation mark (opening/closing) is recognized as closing if
        // followed by whitespace, punctuation or the end of the segment
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201dtext").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019text").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019?").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201ctext").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c?").Build(), 0, 1)
            ),
            Is.True
        );

        // An ambiguous quotation mark (opening/closing) is not recognized as opening if
        // it has leading whitespace
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\t\u201c?").Build(), 1, 2)
            ),
            Is.False
        );
    }

    [Test]
    public void IsMalformedOpeningQuote()
    {
        QuoteConvention centralEuropeanQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("central_european")
        );
        Assert.That(centralEuropeanQuoteConvention, Is.Not.Null);
        var centralEuropeanResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([centralEuropeanQuoteConvention])
        );
        var quotationMarkResolverState = new QuotationMarkResolverState();
        var quotationContinuerState = new QuoteContinuerState();
        var centralEuropeanQuotationMarkCategorizer = new QuotationMarkCategorizer(
            centralEuropeanResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention britishEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("british_english")
        );
        Assert.That(britishEnglishQuoteConvention, Is.Not.Null);
        var britishEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([britishEnglishQuoteConvention])
        );
        var britishEnglishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            britishEnglishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention standardSwedishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_swedish")
        );
        Assert.That(standardSwedishQuoteConvention, Is.Not.Null);
        var standardSwedishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardSwedishQuoteConvention])
        );
        var standardSwedishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            standardSwedishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        var threeConventionsResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([
                centralEuropeanQuoteConvention,
                britishEnglishQuoteConvention,
                standardSwedishQuoteConvention,
            ])
        );
        var threeConventionsQuotationMarkCategorizer = new QuotationMarkCategorizer(
            threeConventionsResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        // It should only accept valid opening marks under the quote convention
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201e ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201a ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018 ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019 ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u00ab ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \" ").Build(), 1, 2)
            ),
            Is.False
        );

        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201e ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201a ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018 ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019 ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u00ab ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \" ").Build(), 1, 2)
            ),
            Is.False
        );

        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201e ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201a ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018 ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019 ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u00ab ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \" ").Build(), 1, 2)
            ),
            Is.False
        );

        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201e ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201a ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018 ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019 ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u00ab ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \" ").Build(), 1, 2)
            ),
            Is.False
        );

        // Should return true if there is a leading quote introducer
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(",\u201d ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019 ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(":\u2019 ").Build(), 1, 2)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(",\u201c ").Build(), 1, 2)
            ),
            Is.True
        );

        // Should return false unless the mark has leading and trailing whitespace
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d ").Build(), 1, 2)
            ),
            Is.True
        );

        // Should return false if there is already an open quotation mark on the stack
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019 ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019 ").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedOpeningQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201c ").Build(), 1, 2)
            ),
            Is.False
        );
    }

    [Test]
    public void IsMalformedClosingQuote()
    {
        QuoteConvention centralEuropeanQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("central_european")
        );
        Assert.That(centralEuropeanQuoteConvention, Is.Not.Null);
        var centralEuropeanResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([centralEuropeanQuoteConvention])
        );
        var quotationMarkResolverState = new QuotationMarkResolverState();
        var quotationContinuerState = new QuoteContinuerState();
        var centralEuropeanQuotationMarkCategorizer = new QuotationMarkCategorizer(
            centralEuropeanResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention britishEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("british_english")
        );
        Assert.That(britishEnglishQuoteConvention, Is.Not.Null);
        var britishEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([britishEnglishQuoteConvention])
        );
        var britishEnglishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            britishEnglishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention standardSwedishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_swedish")
        );
        Assert.That(standardSwedishQuoteConvention, Is.Not.Null);
        var standardSwedishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardSwedishQuoteConvention])
        );
        var standardSwedishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            standardSwedishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        var threeConventionsResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([
                centralEuropeanQuoteConvention,
                britishEnglishQuoteConvention,
                standardSwedishQuoteConvention,
            ])
        );
        var threeConventionsQuotationMarkCategorizer = new QuotationMarkCategorizer(
            threeConventionsResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        // It should only accept valid closing marks under the quote convention
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201e").Build(), 0, 1)
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201e").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201a").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
            ),
            Is.False
        );

        quotationMarkResolverState.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
            ),
            Is.False
        );

        quotationMarkResolverState.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
        );
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
            ),
            Is.False
        );

        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
            ),
            Is.False
        );

        // Returns true if it's at the end of the segment
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );

        // Returns true if it does not have trailing whitespace
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d-").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201dtext").Build(), 0, 1)
            ),
            Is.True
        );

        // Returns true if it has trailing and leading whitespace
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d ").Build(), 1, 2)
            ),
            Is.True
        );

        // Requires there to be an open quotation mark on the stack
        quotationMarkResolverState.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.False
        );

        // Requires the quotation mark on the stack to be a valid pair with the
        // observed quotation mark
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );

        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsMalformedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.True
        );
    }

    [Test]
    public void IsUnpairedClosingQuote()
    {
        QuoteConvention centralEuropeanQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("central_european")
        );
        Assert.That(centralEuropeanQuoteConvention, Is.Not.Null);
        var centralEuropeanResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([centralEuropeanQuoteConvention])
        );
        var quotationMarkResolverState = new QuotationMarkResolverState();
        var quotationContinuerState = new QuoteContinuerState();
        var centralEuropeanQuotationMarkCategorizer = new QuotationMarkCategorizer(
            centralEuropeanResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention britishEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("british_english")
        );
        Assert.That(britishEnglishQuoteConvention, Is.Not.Null);
        var britishEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([britishEnglishQuoteConvention])
        );
        var britishEnglishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            britishEnglishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention standardSwedishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_swedish")
        );
        Assert.That(standardSwedishQuoteConvention, Is.Not.Null);
        var standardSwedishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardSwedishQuoteConvention])
        );
        var standardSwedishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            standardSwedishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        var threeConventionsResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([
                centralEuropeanQuoteConvention,
                britishEnglishQuoteConvention,
                standardSwedishQuoteConvention,
            ])
        );
        var threeConventionsQuotationMarkCategorizer = new QuotationMarkCategorizer(
            threeConventionsResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        // It should only accept valid closing marks under the quote convention
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201e").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201a").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
            ),
            Is.False
        );

        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
            ),
            Is.False
        );

        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
            ),
            Is.False
        );

        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u00bb").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
            ),
            Is.False
        );

        // There must not be an opening quotation mark on the stack
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            centralEuropeanQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            standardSwedishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.False
        );
        Assert.That(
            threeConventionsQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
            ),
            Is.False
        );

        // There must not be leading whitespace
        quotationMarkResolverState.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u201d").Build(), 1, 2)
            ),
            Is.False
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\t\u2019").Build(), 1, 2)
            ),
            Is.False
        );

        // The quotation mark must be either at the end of the segment
        // or have trailing whitespace
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d ").Build(), 0, 1)
            ),
            Is.True
        );
        Assert.That(
            britishEnglishQuotationMarkCategorizer.IsUnpairedClosingQuotationMark(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201d?").Build(), 0, 1)
            ),
            Is.False
        );
    }

    [Test]
    public void IsApostrophe()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var quotationMarkResolverState = new QuotationMarkResolverState();
        var quotationContinuerState = new QuoteContinuerState();
        var standardEnglishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            standardEnglishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        QuoteConvention typewriterEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("typewriter_english")
        );
        Assert.That(typewriterEnglishQuoteConvention, Is.Not.Null);
        var typewriterEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        var typewriterEnglishQuotationMarkCategorizer = new QuotationMarkCategorizer(
            typewriterEnglishResolverSettings,
            quotationMarkResolverState,
            quotationContinuerState
        );

        // The quotation mark must make for a plausible apostrophe
        Assert.That(
            typewriterEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a'b").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            typewriterEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u2019b").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            typewriterEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u2018b").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            typewriterEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u201cb").Build(), 1, 2),
                null
            ),
            Is.False
        );
        Assert.That(
            typewriterEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\"b").Build(), 1, 2),
                null
            ),
            Is.False
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a'b").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u2019b").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u2018b").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u201cb").Build(), 1, 2),
                null
            ),
            Is.False
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\"b").Build(), 1, 2),
                null
            ),
            Is.False
        );

        // Returns true if the mark has Latin letters on both sides
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u2019Ƅ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("ǡ\u2019b").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("ᴀ\u2019Ｂ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("𝼀\u2019Ꝙ").Build(), 1, 2),
                null
            ),
            Is.True
        );

        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u2019ℵ").Build(), 1, 2),
                null
            ),
            Is.False
        );
        Assert.That(
            typewriterEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("a\u2019ℵ").Build(), 1, 2),
                null
            ),
            Is.True
        );

        // Recognizes s possessives (e.G. Moses')
        quotationMarkResolverState.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2019").Build(), 0, 1)
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("s\u2019 ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("Moses\u2019 ").Build(), 5, 6),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("s\u2019?").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("s\u20195").Build(), 1, 2),
                null
            ),
            Is.False
        );

        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("s\u2019 ").Build(), 1, 2),
                null
            ),
            Is.True
        );

        quotationMarkResolverState.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
        );
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u2018").Build(), 0, 1)
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("s\u2019 ").Build(), 1, 2),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word\u2019").Build(), 4, 5)
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("s\u2019 ").Build(), 1, 2),
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("word\u201d").Build(), 4, 5)
            ),
            Is.False
        );

        // the straight quote should always be an apostrophe if it's not a valid quotation mark
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("5'ℵ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" ' ").Build(), 1, 2),
                null
            ),
            Is.True
        );

        // the straight quote should be an apostrophe if there's nothing on the quotation mark stack
        quotationMarkResolverState.AddClosingQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\"").Build(), 0, 1)
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("5'ℵ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" ' ").Build(), 1, 2),
                null
            ),
            Is.True
        );

        // any matching mark should be an apostrophe if it doesn't pair with the
        // deepest opening quotation mark on the stack
        // (opening/closing quotation marks will have been detected before calling this)
        quotationMarkResolverState.AddOpeningQuotationMark(
            new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201c").Build(), 0, 1)
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("5'ℵ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" ' ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("5\u2018ℵ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2018 ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("5\u2019ℵ").Build(), 1, 2),
                null
            ),
            Is.True
        );
        Assert.That(
            standardEnglishQuotationMarkCategorizer.IsApostrophe(
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText(" \u2019 ").Build(), 1, 2),
                null
            ),
            Is.True
        );
    }

    [Test]
    public void DepthBasedQuotationMarkResolverReset()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        standardEnglishQuotationMarkResolver
            .ResolveQuotationMarks([
                new QuotationMarkStringMatch(new TextSegment.Builder().SetText("\u201cThis is a quote").Build(), 0, 1),
            ])
            .ToList();

        Assert.That(
            standardEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.UnpairedQuotationMark])
        );

        standardEnglishQuotationMarkResolver.Reset();
        Assert.That(standardEnglishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));

        standardEnglishQuotationMarkResolver
            .ResolveQuotationMarks([
                new QuotationMarkStringMatch(
                    new TextSegment.Builder().SetText("This is a quote\u2019").Build(),
                    15,
                    16
                ),
            ])
            .ToList();

        Assert.That(
            standardEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.UnpairedQuotationMark])
        );
    }

    [Test]
    public void BasicQuotationMarkRecognition()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment = new TextSegment.Builder().SetText("\u201cThis is a \u2018quote\u2019\u201d").Build();
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 11, 12),
                    new QuotationMarkStringMatch(textSegment, 17, 18),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Opening, textSegment, 11, 12),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Closing, textSegment, 17, 18),
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Closing, textSegment, 18, 19),
                ])
        );
        Assert.That(standardEnglishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void ResolutionOnlyOfPassedMatches()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment = new TextSegment.Builder().SetText("\u201cThis is a \u2018quote\u2019\u201d").Build();
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([new QuotationMarkStringMatch(textSegment, 0, 1)])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                ])
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.UnpairedQuotationMark])
        );

        textSegment = new TextSegment.Builder().SetText("\u201cThis is a \u2018quote\u2019\u201d").Build();
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([new QuotationMarkStringMatch(textSegment, 17, 18)])
                .Count(),
            Is.EqualTo(0)
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.UnpairedQuotationMark])
        );
    }

    [Test]
    public void ResolutionAcrossSegments()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment1 = new TextSegment.Builder().SetText("\u201cThis is a ").Build();
        TextSegment textSegment2 = new TextSegment.Builder().SetText("\u2018quote\u2019\u201d").Build();
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment1, 0, 1),
                    new QuotationMarkStringMatch(textSegment2, 0, 1),
                    new QuotationMarkStringMatch(textSegment2, 6, 7),
                    new QuotationMarkStringMatch(textSegment2, 7, 8),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment1, 0, 1),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Opening, textSegment2, 0, 1),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Closing, textSegment2, 6, 7),
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Closing, textSegment2, 7, 8),
                ])
        );
        Assert.That(standardEnglishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void ResolutionWithApostrophes()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment = (
            new TextSegment.Builder()
                .SetText("\u201cThis\u2019 is a \u2018quote\u2019\u201d")
                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                .Build()
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 5, 6),
                    new QuotationMarkStringMatch(textSegment, 12, 13),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                    new QuotationMarkStringMatch(textSegment, 19, 20),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Opening, textSegment, 12, 13),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Closing, textSegment, 18, 19),
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Closing, textSegment, 19, 20),
                ])
        );
        Assert.That(standardEnglishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));

        QuoteConvention typewriterEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("typewriter_english")
        );
        Assert.That(typewriterEnglishQuoteConvention, Is.Not.Null);
        var typewriterEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        var typewriterEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(
            typewriterEnglishResolverSettings
        );

        textSegment = new TextSegment.Builder()
            .SetText("\"This' is a 'quote'\"")
            .AddPrecedingMarker(UsfmMarkerType.Paragraph)
            .Build();
        Assert.That(
            typewriterEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 5, 6),
                    new QuotationMarkStringMatch(textSegment, 12, 13),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                    new QuotationMarkStringMatch(textSegment, 19, 20),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\"", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("'", 2, QuotationMarkDirection.Opening, textSegment, 12, 13),
                    new QuotationMarkMetadata("'", 2, QuotationMarkDirection.Closing, textSegment, 18, 19),
                    new QuotationMarkMetadata("\"", 1, QuotationMarkDirection.Closing, textSegment, 19, 20),
                ])
        );
        Assert.That(standardEnglishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void EnglishQuoteContinuers()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment1 = new TextSegment.Builder().SetText("\u201cThis is a \u2018quote").Build();
        TextSegment textSegment2 = (
            new TextSegment.Builder()
                .SetText("\u201c\u2018This is the rest\u2019 of it\u201d")
                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                .Build()
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment1, 0, 1),
                    new QuotationMarkStringMatch(textSegment1, 11, 12),
                    new QuotationMarkStringMatch(textSegment2, 0, 1),
                    new QuotationMarkStringMatch(textSegment2, 1, 2),
                    new QuotationMarkStringMatch(textSegment2, 18, 19),
                    new QuotationMarkStringMatch(textSegment2, 25, 26),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment1, 0, 1),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Opening, textSegment1, 11, 12),
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment2, 0, 1),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Opening, textSegment2, 1, 2),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Closing, textSegment2, 18, 19),
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Closing, textSegment2, 25, 26),
                ])
        );
        Assert.That(standardEnglishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void SpanishQuoteContinuers()
    {
        QuoteConvention westernEuropeanQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("western_european")
        );
        Assert.That(westernEuropeanQuoteConvention, Is.Not.Null);
        var westernEuropeanResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([westernEuropeanQuoteConvention])
        );
        var westernEuropeanQuotationMarkResolver = new DepthBasedQuotationMarkResolver(westernEuropeanResolverSettings);

        TextSegment textSegment1 = new TextSegment.Builder().SetText("\u00abThis is a \u201cquote").Build();
        TextSegment textSegment2 = (
            new TextSegment.Builder()
                .SetText("\u00bb\u201dThis is the rest\u201d of it\u00bb")
                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                .Build()
        );
        Assert.That(
            westernEuropeanQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment1, 0, 1),
                    new QuotationMarkStringMatch(textSegment1, 11, 12),
                    new QuotationMarkStringMatch(textSegment2, 0, 1),
                    new QuotationMarkStringMatch(textSegment2, 1, 2),
                    new QuotationMarkStringMatch(textSegment2, 18, 19),
                    new QuotationMarkStringMatch(textSegment2, 25, 26),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u00ab", 1, QuotationMarkDirection.Opening, textSegment1, 0, 1),
                    new QuotationMarkMetadata("\u201c", 2, QuotationMarkDirection.Opening, textSegment1, 11, 12),
                    new QuotationMarkMetadata("\u00bb", 1, QuotationMarkDirection.Opening, textSegment2, 0, 1),
                    new QuotationMarkMetadata("\u201d", 2, QuotationMarkDirection.Opening, textSegment2, 1, 2),
                    new QuotationMarkMetadata("\u201d", 2, QuotationMarkDirection.Closing, textSegment2, 18, 19),
                    new QuotationMarkMetadata("\u00bb", 1, QuotationMarkDirection.Closing, textSegment2, 25, 26),
                ])
        );
        Assert.That(westernEuropeanQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void MalformedQuotationMarks()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment1 = new TextSegment.Builder().SetText("\u201c This is a,\u2018 quote").Build();
        TextSegment textSegment2 = (
            new TextSegment.Builder()
                .SetText("This is the rest \u2019 of it \u201d")
                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                .Build()
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment1, 0, 1),
                    new QuotationMarkStringMatch(textSegment1, 12, 13),
                    new QuotationMarkStringMatch(textSegment2, 17, 18),
                    new QuotationMarkStringMatch(textSegment2, 25, 26),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment1, 0, 1),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Opening, textSegment1, 12, 13),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Closing, textSegment2, 17, 18),
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Closing, textSegment2, 25, 26),
                ])
        );
        Assert.That(standardEnglishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void UnpairedQuotationMarkIssue()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment = new TextSegment.Builder().SetText("\u201cThis is a \u2018quote\u2019").Build();
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 11, 12),
                    new QuotationMarkStringMatch(textSegment, 17, 18),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Opening, textSegment, 11, 12),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Closing, textSegment, 17, 18),
                ])
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.UnpairedQuotationMark])
        );

        textSegment = new TextSegment.Builder().SetText("another quote\u201d").Build();
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([new QuotationMarkStringMatch(textSegment, 13, 14)])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Closing, textSegment, 13, 14),
                ])
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.UnpairedQuotationMark])
        );
    }

    [Test]
    public void TooDeepNestingIssue()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment = new TextSegment.Builder()
            .SetText("\u201cThis \u2018is \u201ca \u2018quote \u201cnested too deeply")
            .Build();
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 6, 7),
                    new QuotationMarkStringMatch(textSegment, 10, 11),
                    new QuotationMarkStringMatch(textSegment, 13, 14),
                    new QuotationMarkStringMatch(textSegment, 20, 21),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Opening, textSegment, 6, 7),
                    new QuotationMarkMetadata("\u201c", 3, QuotationMarkDirection.Opening, textSegment, 10, 11),
                    new QuotationMarkMetadata("\u2018", 4, QuotationMarkDirection.Opening, textSegment, 13, 14),
                ])
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([
                    QuotationMarkResolutionIssue.TooDeepNesting,
                    QuotationMarkResolutionIssue.UnpairedQuotationMark,
                ])
        );
    }

    [Test]
    public void IncompatibleQuotationMarkIssue()
    {
        QuoteConvention standardEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_english")
        );
        Assert.That(standardEnglishQuoteConvention, Is.Not.Null);
        var standardEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardEnglishQuoteConvention])
        );
        var standardEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardEnglishResolverSettings);

        TextSegment textSegment = new TextSegment.Builder().SetText("\u201cThis is a \u201cquote\u201d\u201d").Build();
        Assert.That(
            standardEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 11, 12),
                    new QuotationMarkStringMatch(textSegment, 17, 18),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("\u201c", 2, QuotationMarkDirection.Opening, textSegment, 11, 12),
                    new QuotationMarkMetadata("\u201d", 2, QuotationMarkDirection.Closing, textSegment, 17, 18),
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Closing, textSegment, 18, 19),
                ])
        );
        Assert.That(
            standardEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.IncompatibleQuotationMark])
        );
    }

    [Test]
    public void AmbiguousQuotationMarkIssue()
    {
        QuoteConvention typewriterEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("typewriter_english")
        );
        Assert.That(typewriterEnglishQuoteConvention, Is.Not.Null);
        var typewriterEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        var typewriterEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(
            typewriterEnglishResolverSettings
        );

        TextSegment textSegment = new TextSegment.Builder().SetText("This\"is an ambiguous quotation mark").Build();
        Assert.That(
            typewriterEnglishQuotationMarkResolver
                .ResolveQuotationMarks([new QuotationMarkStringMatch(textSegment, 4, 5)])
                .Count(),
            Is.EqualTo(0)
        );
        Assert.That(
            typewriterEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.AmbiguousQuotationMark])
        );

        typewriterEnglishQuotationMarkResolver.Reset();
        textSegment = new TextSegment.Builder().SetText("\u201cThis is an ambiguous quotation mark").Build();
        Assert.That(
            typewriterEnglishQuotationMarkResolver
                .ResolveQuotationMarks([new QuotationMarkStringMatch(textSegment, 0, 1)])
                .Count(),
            Is.EqualTo(0)
        );
        Assert.That(
            typewriterEnglishQuotationMarkResolver
                .GetIssues()
                .SequenceEqual([QuotationMarkResolutionIssue.AmbiguousQuotationMark])
        );
    }

    [Test]
    public void TypewriterEnglishQuotationMarkRecognition()
    {
        QuoteConvention typewriterEnglishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("typewriter_english")
        );
        Assert.That(typewriterEnglishQuoteConvention, Is.Not.Null);
        var typewriterEnglishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([typewriterEnglishQuoteConvention])
        );
        var typewriterEnglishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(
            typewriterEnglishResolverSettings
        );

        TextSegment textSegment = (
            new TextSegment.Builder()
                .SetText("\"This is a 'quote'\"")
                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                .Build()
        );
        Assert.That(
            typewriterEnglishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 11, 12),
                    new QuotationMarkStringMatch(textSegment, 17, 18),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\"", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("'", 2, QuotationMarkDirection.Opening, textSegment, 11, 12),
                    new QuotationMarkMetadata("'", 2, QuotationMarkDirection.Closing, textSegment, 17, 18),
                    new QuotationMarkMetadata("\"", 1, QuotationMarkDirection.Closing, textSegment, 18, 19),
                ])
        );
        Assert.That(typewriterEnglishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void TypewriterFrenchMarkRecognition()
    {
        QuoteConvention typewriterFrenchQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("typewriter_french")
        );
        Assert.That(typewriterFrenchQuoteConvention, Is.Not.Null);
        var typewriterFrenchResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([typewriterFrenchQuoteConvention])
        );
        var typewriterFrenchQuotationMarkResolver = new DepthBasedQuotationMarkResolver(
            typewriterFrenchResolverSettings
        );

        TextSegment textSegment = new TextSegment.Builder().SetText("<<This is a <quote>>>").Build();
        Assert.That(
            typewriterFrenchQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 2),
                    new QuotationMarkStringMatch(textSegment, 12, 13),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                    new QuotationMarkStringMatch(textSegment, 19, 21),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("<<", 1, QuotationMarkDirection.Opening, textSegment, 0, 2),
                    new QuotationMarkMetadata("<", 2, QuotationMarkDirection.Opening, textSegment, 12, 13),
                    new QuotationMarkMetadata(">", 2, QuotationMarkDirection.Closing, textSegment, 18, 19),
                    new QuotationMarkMetadata(">>", 1, QuotationMarkDirection.Closing, textSegment, 19, 21),
                ])
        );
        Assert.That(typewriterFrenchQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void CentralEuropeanQuotationMarkRecognition()
    {
        QuoteConvention centralEuropeanQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("central_european")
        );
        Assert.That(centralEuropeanQuoteConvention, Is.Not.Null);
        var centralEuropeanResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([centralEuropeanQuoteConvention])
        );
        var centralEuropeanQuotationMarkResolver = new DepthBasedQuotationMarkResolver(centralEuropeanResolverSettings);

        TextSegment textSegment = (
            new TextSegment.Builder()
                .SetText("\u201eThis is a \u201aquote\u2018\u201c")
                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                .Build()
        );
        Assert.That(
            centralEuropeanQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 11, 12),
                    new QuotationMarkStringMatch(textSegment, 17, 18),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201e", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("\u201a", 2, QuotationMarkDirection.Opening, textSegment, 11, 12),
                    new QuotationMarkMetadata("\u2018", 2, QuotationMarkDirection.Closing, textSegment, 17, 18),
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Closing, textSegment, 18, 19),
                ])
        );
        Assert.That(centralEuropeanQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void StandardSwedishQuotationMarkRecognition()
    {
        QuoteConvention standardSwedishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_swedish")
        );
        Assert.That(standardSwedishQuoteConvention, Is.Not.Null);
        var standardSwedishResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([standardSwedishQuoteConvention])
        );
        var standardSwedishQuotationMarkResolver = new DepthBasedQuotationMarkResolver(standardSwedishResolverSettings);

        TextSegment textSegment = (
            new TextSegment.Builder()
                .SetText("\u201dThis is a \u2019quote\u2019\u201d")
                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                .Build()
        );
        Assert.That(
            standardSwedishQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 11, 12),
                    new QuotationMarkStringMatch(textSegment, 17, 18),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Opening, textSegment, 11, 12),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Closing, textSegment, 17, 18),
                    new QuotationMarkMetadata("\u201d", 1, QuotationMarkDirection.Closing, textSegment, 18, 19),
                ])
        );
        Assert.That(standardSwedishQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    [Test]
    public void MultipleConventionsQuotationMarkRecognition()
    {
        QuoteConvention typewriterFrenchQuoteConvention = QuoteConventions.Standard.GetQuoteConventionByName(
            "typewriter_french"
        );

        Assert.That(typewriterFrenchQuoteConvention, Is.Not.Null);

        QuoteConvention centralEuropeanQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("central_european")
        );
        Assert.That(centralEuropeanQuoteConvention, Is.Not.Null);

        QuoteConvention standardSwedishQuoteConvention = (
            QuoteConventions.Standard.GetQuoteConventionByName("standard_swedish")
        );
        Assert.That(standardSwedishQuoteConvention, Is.Not.Null);
        var multipleConventionsResolverSettings = new QuoteConventionDetectionResolutionSettings(
            new QuoteConventionSet([
                typewriterFrenchQuoteConvention,
                centralEuropeanQuoteConvention,
                standardSwedishQuoteConvention,
            ])
        );
        var multipleConventionsQuotationMarkResolver = new DepthBasedQuotationMarkResolver(
            multipleConventionsResolverSettings
        );

        TextSegment textSegment = (
            new TextSegment.Builder()
                .SetText("\u201eThis is a \u2019quote>\u201c")
                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                .Build()
        );
        Assert.That(
            multipleConventionsQuotationMarkResolver
                .ResolveQuotationMarks([
                    new QuotationMarkStringMatch(textSegment, 0, 1),
                    new QuotationMarkStringMatch(textSegment, 11, 12),
                    new QuotationMarkStringMatch(textSegment, 17, 18),
                    new QuotationMarkStringMatch(textSegment, 18, 19),
                ])
                .SequenceEqual([
                    new QuotationMarkMetadata("\u201e", 1, QuotationMarkDirection.Opening, textSegment, 0, 1),
                    new QuotationMarkMetadata("\u2019", 2, QuotationMarkDirection.Opening, textSegment, 11, 12),
                    new QuotationMarkMetadata(">", 2, QuotationMarkDirection.Closing, textSegment, 17, 18),
                    new QuotationMarkMetadata("\u201c", 1, QuotationMarkDirection.Closing, textSegment, 18, 19),
                ])
        );
        Assert.That(multipleConventionsQuotationMarkResolver.GetIssues(), Has.Count.EqualTo(0));
    }

    private class TestQuoteContinuerState : QuoteContinuerState
    {
        public QuoteContinuerStyle InternalContinuerStyle
        {
            get => ContinuerStyle;
            set => ContinuerStyle = value;
        }
    }
}
