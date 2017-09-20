using NUnit.Framework;

namespace SIL.Machine.Tokenization
{
	[TestFixture]
	public class LineSegmentTokenizerTests
	{
		[Test]
		public void Tokenize_Empty_ReturnsEmpty()
		{
			var tokenizer = new LineSegmentTokenizer();
			Assert.That(tokenizer.TokenizeToStrings(""), Is.Empty);
		}

		[Test]
		public void Tokenize_SingleLine_ReturnsTokens()
		{
			var tokenizer = new LineSegmentTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is a test."), Is.EqualTo(new[] { "This is a test." }));
		}

		[Test]
		public void Tokenize_MultipleLines_ReturnsTokens()
		{
			var tokenizer = new LineSegmentTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is the first sentence.\nThis is the second sentence."),
				Is.EqualTo(new[] { "This is the first sentence.", "This is the second sentence." }));
		}

		[Test]
		public void Tokenize_EndsWithNewLine_ReturnsTokens()
		{
			var tokenizer = new LineSegmentTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is a test.\n"), Is.EqualTo(new[] { "This is a test." }));
		}

		[Test]
		public void Tokenize_EndsWithNewLineAndSpace_ReturnsTokens()
		{
			var tokenizer = new LineSegmentTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is a test.\n "), Is.EqualTo(new[] { "This is a test." }));
		}

		[Test]
		public void Tokenize_EndsWithTextAndSpace_ReturnsTokens()
		{
			var tokenizer = new LineSegmentTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is the first sentence.\nThis is a partial sentence "),
				Is.EqualTo(new[] { "This is the first sentence.", "This is a partial sentence " }));
		}

		[Test]
		public void Tokenize_EmptyLine_ReturnsTokens()
		{
			var tokenizer = new LineSegmentTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is the first sentence.\n\nThis is the third sentence."),
				Is.EqualTo(new[] { "This is the first sentence.", "", "This is the third sentence." }));
		}
	}
}
