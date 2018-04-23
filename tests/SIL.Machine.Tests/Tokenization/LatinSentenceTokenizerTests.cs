using NUnit.Framework;

namespace SIL.Machine.Tokenization
{
	[TestFixture]
	public class LatinSentenceTokenizerTests
	{
		[Test]
		public void Tokenize_Empty_ReturnsEmpty()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings(""), Is.Empty);
		}

		[Test]
		public void Tokenize_SingleLine_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is a test."), Is.EqualTo(new[] { "This is a test." }));
		}

		[Test]
		public void Tokenize_MultipleLines_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is the first sentence.\nThis is the second sentence."),
				Is.EqualTo(new[] { "This is the first sentence.", "This is the second sentence." }));
		}

		[Test]
		public void Tokenize_TwoSentences_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is the first sentence. This is the second sentence."),
				Is.EqualTo(new[] { "This is the first sentence.", "This is the second sentence." }));
		}

		[Test]
		public void Tokenize_Quotes_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("\"This is the first sentence.\" This is the second sentence."),
				Is.EqualTo(new[] { "\"This is the first sentence.\"", "This is the second sentence." }));
		}

		[Test]
		public void Tokenize_QuotationInSentence_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("\"This is the first sentence!\" he said. This is the second sentence."),
				Is.EqualTo(new[] { "\"This is the first sentence!\" he said.", "This is the second sentence." }));
		}

		[Test]
		public void Tokenize_Parens_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is the first sentence. (This is the second sentence.)"),
				Is.EqualTo(new[] { "This is the first sentence.", "(This is the second sentence.)" }));
		}

		[Test]
		public void Tokenize_Abbreviation_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer(new[] { "mr", "dr", "ms" });
			Assert.That(tokenizer.TokenizeToStrings("Mr. Smith went to Washington. This is the second sentence."),
				Is.EqualTo(new[] { "Mr. Smith went to Washington.", "This is the second sentence." }));
		}

		[Test]
		public void Tokenize_IncompleteSentence_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is an incomplete sentence "),
				Is.EqualTo(new[] { "This is an incomplete sentence " }));
		}

		[Test]
		public void Tokenize_CompleteSentenceWithSpaceAtEnd_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("\"This is a complete sentence.\" \n"),
				Is.EqualTo(new[] { "\"This is a complete sentence.\"" }));
		}
	}
}
