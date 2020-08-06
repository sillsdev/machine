using NUnit.Framework;

namespace SIL.Machine.Tokenization
{
	[TestFixture]
	public class LatinWordTokenizerTests
	{
		[Test]
		public void Tokenize_Empty()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings(""), Is.Empty);
		}

		[Test]
		public void Tokenize_Whitespace()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings(" "), Is.Empty);
		}

		[Test]
		public void Tokenize_PunctuationAtEndOfWord()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is a test, also."),
				Is.EqualTo(new[] { "This", "is", "a", "test", ",", "also", "." }));
		}

		[Test]
		public void Tokenize_PunctuationAtStartOfWord()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("Is this a test? (yes)"),
				Is.EqualTo(new[] { "Is", "this", "a", "test", "?", "(", "yes", ")" }));
		}

		[Test]
		public void Tokenize_PunctuationInsideWord()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This isn't a test."),
				Is.EqualTo(new[] { "This", "isn't", "a", "test", "." }));

			Assert.That(tokenizer.TokenizeToStrings("He had $5,000."),
				Is.EqualTo(new[] { "He", "had", "$", "5,000", "." }));
		}

		[Test]
		public void Tokenize_Symbol()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("He had $50."),
				Is.EqualTo(new[] { "He", "had", "$", "50", "." }));
		}

		[Test]
		public void Tokenize_Abbreviation()
		{
			var tokenizer = new LatinWordTokenizer(new[] { "mr", "dr", "ms" });
			Assert.That(tokenizer.TokenizeToStrings("Mr. Smith went to Washington."),
				Is.EqualTo(new[] { "Mr.", "Smith", "went", "to", "Washington", "." }));
		}

		[Test]
		public void Tokenize_Quotes()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("\"This is a test.\""),
				Is.EqualTo(new[] { "\"", "This", "is", "a", "test", ".", "\"" }));
		}

		[Test]
		public void Tokenize_ApostropheNotAsSingleQuote()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("“Moses' cat said ‘Meow’ to the dog.”"),
				Is.EqualTo(new[] { "“", "Moses'", "cat", "said", "‘", "Meow", "’", "to", "the", "dog", ".", "”" }));

			Assert.That(tokenizer.TokenizeToStrings("i ha''on 'ot ano'."),
				Is.EqualTo(new[] { "i", "ha''on", "'ot", "ano'", "." }));
		}

		[Test]
		public void Tokenize_ApostropheAsSingleQuote()
		{
			var tokenizer = new LatinWordTokenizer { TreatApostropheAsSingleQuote = true };
			Assert.That(tokenizer.TokenizeToStrings("'Moses's cat said 'Meow' to the dog.'"),
				Is.EqualTo(new[] { "'", "Moses's", "cat", "said", "'", "Meow", "'", "to", "the", "dog", ".", "'" }));
		}

		[Test]
		public void Tokenize_Slash()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is a test/trial."),
				Is.EqualTo(new[] { "This", "is", "a", "test", "/", "trial", "." }));
		}

		[Test]
		public void Tokenize_AngleBracket()
		{
			var tokenizer = new LatinWordTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is a <<test>>."),
				Is.EqualTo(new[] { "This", "is", "a", "<<", "test", ">>", "." }));
		}
	}
}
