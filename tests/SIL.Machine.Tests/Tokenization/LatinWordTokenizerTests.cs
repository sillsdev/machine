using NUnit.Framework;

namespace SIL.Machine.Tokenization
{
	[TestFixture]
	public class LatinWordTokenizerTests
	{
		[Test]
		public void Tokenize_Empty_ReturnsEmpty()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings(""), Is.Empty);
		}

		[Test]
		public void Tokenize_Whitespace_ReturnsEmpty()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings(" "), Is.Empty);
		}

		[Test]
		public void Tokenize_PunctuationAtEndOfWord_ReturnsTokens()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is a test."), Is.EqualTo(new[] { "This", "is", "a", "test", "." }));
		}

		[Test]
		public void Tokenize_PunctuationAtStartOfWord_ReturnsTokens()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("Is this a \"test\"?"),
				Is.EqualTo(new[] { "Is", "this", "a", "\"", "test", "\"", "?" }));
		}

		[Test]
		public void Tokenize_PunctuationInsideWord_ReturnsTokens()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This isn't a test."),
				Is.EqualTo(new[] { "This", "isn't", "a", "test", "." }));
		}

		[Test]
		public void Tokenize_Abbreviation_ReturnsTokens()
		{
			var tokenizer = new LatinWordTokenizer(new[] { "mr", "dr", "ms" });
			Assert.That(tokenizer.TokenizeToStrings("Mr. Smith went to Washington."),
				Is.EqualTo(new[] { "Mr.", "Smith", "went", "to", "Washington", "." }));
		}

		[Test]
		public void Tokenize_Quotes_ReturnsTokens()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("\"This is a test.\""),
				Is.EqualTo(new[] { "\"", "This", "is", "a", "test", ".", "\"" }));
		}

		[Test]
		public void Tokenize_ApostropheNotAsSingleQuote_ReturnsTokens()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("“Moses' cat said ‘Meow’ to the dog.”"),
				Is.EqualTo(new[] { "“", "Moses'", "cat", "said", "‘", "Meow", "’", "to", "the", "dog", ".", "”" }));

			Assert.That(tokenizer.TokenizeToStrings("i ha''on ot ano'."),
				Is.EqualTo(new[] { "i", "ha''on", "ot", "ano'", "." }));
		}

		[Test]
		public void Tokenize_ApostropheAsSingleQuote_ReturnsTokens()
		{
			var tokenizer = new LatinWordTokenizer { TreatApostropheAsSingleQuote = true };
			Assert.That(tokenizer.TokenizeToStrings("'Moses's cat said 'Meow' to the dog.'"),
				Is.EqualTo(new[] { "'", "Moses's", "cat", "said", "'", "Meow", "'", "to", "the", "dog", ".", "'" }));
		}
	}
}
