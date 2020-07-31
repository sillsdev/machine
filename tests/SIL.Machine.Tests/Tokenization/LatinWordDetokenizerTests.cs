using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Tokenization
{
	[TestFixture]
	public class LatinWordDetokenizerTests
	{
		[Test]
		public void Detokenize_Empty()
		{
			var detokenizer = new LatinWordDetokenizer();
			Assert.That(detokenizer.Detokenize(Enumerable.Empty<string>()), Is.EqualTo(""));
		}

		[Test]
		public void Detokenize_PunctuationAtEndOfWord()
		{
			var detokenizer = new LatinWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "This", "is", "a", "test", ",", "also", "." }),
				Is.EqualTo("This is a test, also."));
		}

		[Test]
		public void Detokenize_PunctuationAtStartOfWord()
		{
			var detokenizer = new LatinWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "Is", "this", "a", "test", "?", "(", "yes", ")" }),
				Is.EqualTo("Is this a test? (yes)"));
		}

		[Test]
		public void Detokenize_CurrencySymbol()
		{
			var detokenizer = new LatinWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "He", "had", "$", "50", "." }),
				Is.EqualTo("He had $50."));
		}

		[Test]
		public void Detokenize_Quotes()
		{
			var detokenizer = new LatinWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "\"", "This", "is", "a", "test", ".", "\"" }),
				Is.EqualTo("\"This is a test.\""));
		}

		[Test]
		public void Detokenize_MultipleQuotes()
		{
			var detokenizer = new LatinWordDetokenizer();
			Assert.That(detokenizer.Detokenize(
				new[] { "“", "‘", "Moses'", "’", "cat", "said", "‘", "Meow", "’", "to", "the", "dog", ".", "”" }),
				Is.EqualTo("“‘Moses'’ cat said ‘Meow’ to the dog.”"));

			Assert.That(detokenizer.Detokenize(
				new[] { "\"", "Moses's", "cat", "said", "'", "Meow", "'", "to", "the", "dog", ".", "\"" }),
				Is.EqualTo("\"Moses's cat said 'Meow' to the dog.\""));
		}

		[Test]
		public void Detokenize_Slash()
		{
			var detokenizer = new LatinWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "This", "is", "a", "test", "/", "trial", "." }),
				Is.EqualTo("This is a test/trial."));
		}

		[Test]
		public void Detokenize_AngleBracket()
		{
			var detokenizer = new LatinWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "This", "is", "a", "<", "<", "test", ">", ">", "." }),
				Is.EqualTo("This is a <<test>>."));
		}
	}
}
