using NUnit.Framework;
using SIL.Machine.Annotations;

namespace SIL.Machine.Translation;

[TestFixture]
public class PhraseTranslationSuggesterTests
{
    [Test]
    public void GetSuggestion_Punctuation()
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
    public void GetSuggestion_UntranslatedWord()
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
    public void GetSuggestion_PrefixIncompleteWord()
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
    public void GetSuggestion_PrefixCompleteWord()
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
    public void GetSuggestion_PrefixPartialWord()
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
    public void GetSuggestion_BelowThreshold()
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
