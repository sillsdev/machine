using NUnit.Framework;
using SIL.Machine.Annotations;

namespace SIL.Machine.Translation;

[TestFixture]
public class PhraseTranslationSuggesterTests
{
    [Test]
    public void GetSuggestions_Punctuation()
    {
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", "." });
        builder.AppendToken("this", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 1,
            prefixCount: 0,
            isLastWordComplete: true,
            new[] { builder.ToResult() }
        );
        Assert.That(suggestions[0].TargetWords, Is.EqualTo(new[] { "this", "is", "a", "test" }));
    }

    [Test]
    public void GetSuggestions_UntranslatedWord()
    {
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", "." });
        builder.AppendToken("this", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 2),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        builder.AppendToken("a", TranslationSources.None, 0);
        builder.MarkPhrase(
            Range<int>.Create(2, 3),
            new WordAlignmentMatrix(rowCount: 1, columnCount: 1, setValues: new[] { (0, 0) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 1,
            prefixCount: 0,
            isLastWordComplete: true,
            new[] { builder.ToResult() }
        );
        Assert.That(suggestions[0].TargetWords, Is.EqualTo(new[] { "this", "is" }));
    }

    [Test]
    public void GetSuggestions_PrefixIncompleteWord()
    {
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", "." });
        builder.AppendToken("this", TranslationSources.Smt | TranslationSources.Prefix, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 1,
            prefixCount: 1,
            isLastWordComplete: false,
            new[] { builder.ToResult() }
        );
        Assert.That(suggestions[0].TargetWords, Is.EqualTo(new[] { "this", "is", "a", "test" }));
    }

    [Test]
    public void GetSuggestions_PrefixCompleteWord()
    {
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", "." });
        builder.AppendToken("this", TranslationSources.Smt | TranslationSources.Prefix, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 1,
            prefixCount: 1,
            isLastWordComplete: true,
            new[] { builder.ToResult() }
        );
        Assert.That(suggestions[0].TargetWords, Is.EqualTo(new[] { "is", "a", "test" }));
    }

    [Test]
    public void GetSuggestions_PrefixPartialWord()
    {
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", "." });
        builder.AppendToken("te", TranslationSources.Prefix, -1);
        builder.AppendToken("this", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 4, setValues: new[] { (0, 1), (1, 2), (2, 3) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 1,
            prefixCount: 1,
            isLastWordComplete: false,
            new[] { builder.ToResult() }
        );
        Assert.That(suggestions, Is.Empty);
    }

    [Test]
    public void GetSuggestions_Multiple()
    {
        var results = new List<TranslationResult>();
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", "." });
        builder.AppendToken("this", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        results.Add(builder.ToResult());

        builder.Reset();
        builder.AppendToken("that", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        results.Add(builder.ToResult());

        builder.Reset();
        builder.AppendToken("other", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        results.Add(builder.ToResult());

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 2,
            prefixCount: 0,
            isLastWordComplete: true,
            results
        );
        Assert.That(suggestions, Has.Count.EqualTo(2));
        Assert.That(suggestions[0].TargetWords, Is.EqualTo(new[] { "this", "is", "a", "test" }));
        Assert.That(suggestions[1].TargetWords, Is.EqualTo(new[] { "that", "is", "a", "test" }));
    }

    [Test]
    public void GetSuggestions_Duplicate()
    {
        var results = new List<TranslationResult>();
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", ".", "segunda", "frase" });
        builder.AppendToken("this", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        builder.AppendToken("second", TranslationSources.Smt, 0.1);
        builder.AppendToken("sentence", TranslationSources.Smt, 0.1);
        builder.MarkPhrase(
            Range<int>.Create(5, 7),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        results.Add(builder.ToResult());

        builder.Reset();
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 2, setValues: new[] { (1, 0), (2, 1) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        builder.AppendToken("second", TranslationSources.Smt, 0.1);
        builder.AppendToken("sentence", TranslationSources.Smt, 0.1);
        builder.MarkPhrase(
            Range<int>.Create(5, 7),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        results.Add(builder.ToResult());

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 2,
            prefixCount: 0,
            isLastWordComplete: true,
            results
        );
        Assert.That(suggestions, Has.Count.EqualTo(1));
        Assert.That(suggestions[0].TargetWords, Is.EqualTo(new[] { "this", "is", "a", "test" }));
    }

    [Test]
    public void GetSuggestions_StartsWithPunctuation()
    {
        var results = new List<TranslationResult>();
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", "." });
        builder.AppendToken(",", TranslationSources.Smt, 0.5);
        builder.AppendToken("this", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 4, setValues: new[] { (0, 1), (1, 2), (2, 3) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        results.Add(builder.ToResult());

        builder.Reset();
        builder.AppendToken("this", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 5),
            new WordAlignmentMatrix(rowCount: 2, columnCount: 2, setValues: new[] { (0, 0), (1, 1) })
        );
        results.Add(builder.ToResult());

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 2,
            prefixCount: 0,
            isLastWordComplete: true,
            results
        );
        Assert.That(suggestions, Has.Count.EqualTo(1));
        Assert.That(suggestions[0].TargetWords, Is.EqualTo(new[] { "this", "is", "a", "test" }));
    }

    [Test]
    public void GetSuggestions_BelowThreshold()
    {
        var builder = new TranslationResultBuilder(new[] { "esto", "es", "una", "prueba", "." });
        builder.AppendToken("this", TranslationSources.Smt, 0.5);
        builder.AppendToken("is", TranslationSources.Smt, 0.5);
        builder.AppendToken("a", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(0, 3),
            new WordAlignmentMatrix(rowCount: 3, columnCount: 3, setValues: new[] { (0, 0), (1, 1), (2, 2) })
        );
        builder.AppendToken("bad", TranslationSources.Smt, 0.1);
        builder.AppendToken("test", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(3, 4),
            new WordAlignmentMatrix(rowCount: 1, columnCount: 2, setValues: new[] { (0, 1) })
        );
        builder.AppendToken(".", TranslationSources.Smt, 0.5);
        builder.MarkPhrase(
            Range<int>.Create(4, 5),
            new WordAlignmentMatrix(rowCount: 1, columnCount: 1, setValues: new[] { (0, 0) })
        );

        var suggester = new PhraseTranslationSuggester { ConfidenceThreshold = 0.2 };
        IReadOnlyList<TranslationSuggestion> suggestions = suggester.GetSuggestions(
            n: 1,
            prefixCount: 0,
            isLastWordComplete: true,
            new[] { builder.ToResult() }
        );
        Assert.That(suggestions[0].TargetWords, Is.EqualTo(new[] { "this", "is", "a" }));
    }
}
