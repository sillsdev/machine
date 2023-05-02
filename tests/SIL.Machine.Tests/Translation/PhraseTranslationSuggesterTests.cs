using NUnit.Framework;
using SIL.Machine.Annotations;

namespace SIL.Machine.Translation;

[TestFixture]
public class PhraseTranslationSuggesterTests
{
    [Test]
    public void GetSuggestion_Punctuation()
    {
        var builder = new TranslationResultBuilder("esto es una prueba .".Split());
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
        Assert.That(
            suggester.GetSuggestion(prefixCount: 0, isLastWordComplete: true, builder.ToResult()).TargetWordIndices,
            Is.EqualTo(new[] { 0, 1, 2, 3 })
        );
    }

    [Test]
    public void GetSuggestion_UntranslatedWord()
    {
        var builder = new TranslationResultBuilder("esto es una prueba .".Split());
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
        Assert.That(
            suggester.GetSuggestion(prefixCount: 0, isLastWordComplete: true, builder.ToResult()).TargetWordIndices,
            Is.EqualTo(new[] { 0, 1 })
        );
    }

    [Test]
    public void GetSuggestion_PrefixCompletedWord()
    {
        var builder = new TranslationResultBuilder("esto es una prueba .".Split());
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
        Assert.That(
            suggester.GetSuggestion(prefixCount: 1, isLastWordComplete: false, builder.ToResult()).TargetWordIndices,
            Is.EqualTo(new[] { 0, 1, 2, 3 })
        );
    }

    [Test]
    public void GetSuggestion_PrefixPartialWord()
    {
        var builder = new TranslationResultBuilder("esto es una prueba .".Split());
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
        Assert.That(
            suggester.GetSuggestion(prefixCount: 1, isLastWordComplete: false, builder.ToResult()).TargetWordIndices,
            Is.Empty
        );
    }

    [Test]
    public void GetSuggestion_BelowThreshold()
    {
        var builder = new TranslationResultBuilder("esto es una prueba .".Split());
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
        Assert.That(
            suggester.GetSuggestion(prefixCount: 0, isLastWordComplete: true, builder.ToResult()).TargetWordIndices,
            Is.EqualTo(new[] { 0, 1, 2 })
        );
    }
}
