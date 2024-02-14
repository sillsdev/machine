using NUnit.Framework;

namespace SIL.Machine.Tokenization;

[TestFixture]
public class LatinSentenceTokenizerTests
{
    [Test]
    public void Tokenize_Empty()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(tokenizer.Tokenize(""), Is.Empty);
    }

    [Test]
    public void Tokenize_SingleLine()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(tokenizer.Tokenize("This is a test."), Is.EqualTo(new[] { "This is a test." }));
    }

    [Test]
    public void Tokenize_MultipleLines()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is the first sentence.\nThis is the second sentence."),
            Is.EqualTo(new[] { "This is the first sentence.", "This is the second sentence." })
        );
    }

    [Test]
    public void Tokenize_TwoSentences()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is the first sentence. This is the second sentence."),
            Is.EqualTo(new[] { "This is the first sentence.", "This is the second sentence." })
        );
    }

    [Test]
    public void Tokenize_Quotes()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(
            tokenizer.Tokenize("\"This is the first sentence.\" This is the second sentence."),
            Is.EqualTo(new[] { "\"This is the first sentence.\"", "This is the second sentence." })
        );
    }

    [Test]
    public void Tokenize_QuotationInSentence()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(
            tokenizer.Tokenize("\"This is the first sentence!\" he said. This is the second sentence."),
            Is.EqualTo(new[] { "\"This is the first sentence!\" he said.", "This is the second sentence." })
        );
    }

    [Test]
    public void Tokenize_Parens()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is the first sentence. (This is the second sentence.)"),
            Is.EqualTo(new[] { "This is the first sentence.", "(This is the second sentence.)" })
        );
    }

    [Test]
    public void Tokenize_Abbreviation()
    {
        var tokenizer = new LatinSentenceTokenizer(new[] { "mr", "dr", "ms" });
        Assert.That(
            tokenizer.Tokenize("Mr. Smith went to Washington. This is the second sentence."),
            Is.EqualTo(new[] { "Mr. Smith went to Washington.", "This is the second sentence." })
        );
    }

    [Test]
    public void Tokenize_IncompleteSentence()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is an incomplete sentence "),
            Is.EqualTo(new[] { "This is an incomplete sentence " })
        );
    }

    [Test]
    public void Tokenize_CompleteSentenceWithSpaceAtEnd()
    {
        var tokenizer = new LatinSentenceTokenizer();
        Assert.That(
            tokenizer.Tokenize("\"This is a complete sentence.\" \n"),
            Is.EqualTo(new[] { "\"This is a complete sentence.\"" })
        );
    }
}
