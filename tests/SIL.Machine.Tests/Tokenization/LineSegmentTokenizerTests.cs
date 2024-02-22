using NUnit.Framework;

namespace SIL.Machine.Tokenization;

[TestFixture]
public class LineSegmentTokenizerTests
{
    [Test]
    public void Tokenize_Empty()
    {
        var tokenizer = new LineSegmentTokenizer();
        Assert.That(tokenizer.Tokenize(""), Is.Empty);
    }

    [Test]
    public void Tokenize_SingleLine()
    {
        var tokenizer = new LineSegmentTokenizer();
        Assert.That(tokenizer.Tokenize("This is a test."), Is.EqualTo(new[] { "This is a test." }));
    }

    [Test]
    public void Tokenize_MultipleLines()
    {
        var tokenizer = new LineSegmentTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is the first sentence.\nThis is the second sentence."),
            Is.EqualTo(new[] { "This is the first sentence.", "This is the second sentence." })
        );
    }

    [Test]
    public void Tokenize_EndsWithNewLine()
    {
        var tokenizer = new LineSegmentTokenizer();
        Assert.That(tokenizer.Tokenize("This is a test.\n"), Is.EqualTo(new[] { "This is a test." }));
    }

    [Test]
    public void Tokenize_EndsWithNewLineAndSpace()
    {
        var tokenizer = new LineSegmentTokenizer();
        Assert.That(tokenizer.Tokenize("This is a test.\n "), Is.EqualTo(new[] { "This is a test.", " " }));
    }

    [Test]
    public void Tokenize_EndsWithTextAndSpace()
    {
        var tokenizer = new LineSegmentTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is the first sentence.\nThis is a partial sentence "),
            Is.EqualTo(new[] { "This is the first sentence.", "This is a partial sentence " })
        );
    }

    [Test]
    public void Tokenize_EmptyLine()
    {
        var tokenizer = new LineSegmentTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is the first sentence.\n\nThis is the third sentence."),
            Is.EqualTo(new[] { "This is the first sentence.", "", "This is the third sentence." })
        );
    }

    [Test]
    public void Tokenize_LineEndsWithSpace()
    {
        var tokenizer = new LineSegmentTokenizer();
        Assert.That(
            tokenizer.Tokenize("This is the first sentence. \nThis is the second sentence."),
            Is.EqualTo(new[] { "This is the first sentence. ", "This is the second sentence." })
        );
    }
}
