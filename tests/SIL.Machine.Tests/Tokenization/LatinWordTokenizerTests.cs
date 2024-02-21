﻿using NUnit.Framework;

namespace SIL.Machine.Tokenization;

[TestFixture]
public class LatinWordTokenizerTests
{
    [Test]
    public void Tokenize_Empty()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(tokenizer.Tokenize(""), Is.Empty);
    }

    [Test]
    public void Tokenize_Whitespace()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(tokenizer.Tokenize(" "), Is.Empty);
    }

    [Test]
    public void Tokenize_PunctuationAtEndOfWord()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is a test, also."),
            Is.EqualTo(new[] { "This", "is", "a", "test", ",", "also", "." })
        );
    }

    [Test]
    public void Tokenize_PunctuationAtStartOfWord()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("Is this a test? (yes)"),
            Is.EqualTo(new[] { "Is", "this", "a", "test", "?", "(", "yes", ")" })
        );
    }

    [Test]
    public void Tokenize_PunctuationInsideWord()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(tokenizer.Tokenize("This isn't a test."), Is.EqualTo(new[] { "This", "isn't", "a", "test", "." }));

        Assert.That(tokenizer.Tokenize("He had $5,000."), Is.EqualTo(new[] { "He", "had", "$", "5,000", "." }));
    }

    [Test]
    public void Tokenize_Symbol()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(tokenizer.Tokenize("He had $50."), Is.EqualTo(new[] { "He", "had", "$", "50", "." }));
    }

    [Test]
    public void Tokenize_Abbreviation()
    {
        var tokenizer = new LatinWordTokenizer(new[] { "mr", "dr", "ms" });
        Assert.That(
            tokenizer.Tokenize("Mr. Smith went to Washington."),
            Is.EqualTo(new[] { "Mr.", "Smith", "went", "to", "Washington", "." })
        );
    }

    [Test]
    public void Tokenize_Quotes()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("\"This is a test.\""),
            Is.EqualTo(new[] { "\"", "This", "is", "a", "test", ".", "\"" })
        );
    }

    [Test]
    public void Tokenize_ApostropheNotAsSingleQuote()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("“Moses' cat said ‘Meow’ to the dog.”"),
            Is.EqualTo(new[] { "“", "Moses'", "cat", "said", "‘", "Meow", "’", "to", "the", "dog", ".", "”" })
        );

        Assert.That(tokenizer.Tokenize("i ha''on 'ot ano'."), Is.EqualTo(new[] { "i", "ha''on", "'ot", "ano'", "." }));
    }

    [Test]
    public void Tokenize_ApostropheAsSingleQuote()
    {
        var tokenizer = new LatinWordTokenizer { TreatApostropheAsSingleQuote = true };
        Assert.That(
            tokenizer.Tokenize("'Moses's cat said 'Meow' to the dog.'"),
            Is.EqualTo(new[] { "'", "Moses's", "cat", "said", "'", "Meow", "'", "to", "the", "dog", ".", "'" })
        );
    }

    [Test]
    public void Tokenize_Slash()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is a test/trial."),
            Is.EqualTo(new[] { "This", "is", "a", "test", "/", "trial", "." })
        );
    }

    [Test]
    public void Tokenize_AngleBracket()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is a <<test>>."),
            Is.EqualTo(new[] { "This", "is", "a", "<<", "test", ">>", "." })
        );
    }

    [Test]
    public void Tokenize_EmailAddress()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is an email address, name@test.com, in a sentence."),
            Is.EqualTo(
                new[] { "This", "is", "an", "email", "address", ",", "name@test.com", ",", "in", "a", "sentence", "." }
            )
        );
    }

    [Test]
    public void Tokenize_EmailAddressAtEndOfSentence()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("Here is an email address: name@test.com."),
            Is.EqualTo(new[] { "Here", "is", "an", "email", "address", ":", "name@test.com", "." })
        );
    }

    [Test]
    public void Tokenize_Url()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is a url, http://www.test.com/page.html, in a sentence."),
            Is.EqualTo(
                new[]
                {
                    "This",
                    "is",
                    "a",
                    "url",
                    ",",
                    "http://www.test.com/page.html",
                    ",",
                    "in",
                    "a",
                    "sentence",
                    "."
                }
            )
        );
    }

    [Test]
    public void Tokenize_UrlAtEndOfSentence()
    {
        var tokenizer = new LatinWordTokenizer();
        Assert.That(
            tokenizer.Tokenize("Here is a url: http://www.test.com/page.html?param=1."),
            Is.EqualTo(new[] { "Here", "is", "a", "url", ":", "http://www.test.com/page.html?param=1", "." })
        );
    }
}
