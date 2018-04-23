using System.Linq;
using Bridge.Html5;
using Bridge.QUnit;

namespace SIL.Machine.Tokenization
{
	public class LatinWordTokenizerTests
	{
		[Ready]
		public static void RunTests()
		{
			QUnit.Module(nameof(LatinWordTokenizerTests));

			QUnit.Test(nameof(Tokenize_Empty_ReturnsEmpty), Tokenize_Empty_ReturnsEmpty);
			QUnit.Test(nameof(Tokenize_Whitespace_ReturnsEmpty), Tokenize_Whitespace_ReturnsEmpty);
			QUnit.Test(nameof(Tokenize_PunctuationAtEndOfWord_ReturnsTokens), Tokenize_PunctuationAtEndOfWord_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_PunctuationAtStartOfWord_ReturnsTokens), Tokenize_PunctuationAtStartOfWord_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_PunctuationInsideWord_ReturnsTokens), Tokenize_PunctuationInsideWord_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_Abbreviation_ReturnsTokens), Tokenize_Abbreviation_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_NonAsciiCharacter_DoesNotThrow), Tokenize_NonAsciiCharacter_DoesNotThrow);
		}

		private static void Tokenize_Empty_ReturnsEmpty(Assert assert)
		{
			var tokenizer = new LatinWordTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("").ToArray(), new string[0]);
		}

		private static void Tokenize_Whitespace_ReturnsEmpty(Assert assert)
		{
			var tokenizer = new LatinWordTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(" ").ToArray(), new string[0]);
		}

		private static void Tokenize_PunctuationAtEndOfWord_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinWordTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This is a test.").ToArray(), new[] {"This", "is", "a", "test", "."});
		}

		private static void Tokenize_PunctuationAtStartOfWord_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinWordTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("Is this a \"test\"?").ToArray(),
				new[] {"Is", "this", "a", "\"", "test", "\"", "?"});
		}

		private static void Tokenize_PunctuationInsideWord_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinWordTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This isn't a test.").ToArray(),
				new[] {"This", "isn't", "a", "test", "."});
		}

		private static void Tokenize_Abbreviation_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinWordTokenizer(new[] { "mr", "dr", "ms" });
			assert.DeepEqual(tokenizer.TokenizeToStrings("Mr. Smith went to Washington.").ToArray(),
				new[] {"Mr.", "Smith", "went", "to", "Washington", "."});
		}

		/// <summary>  
		/// This tests a workaround for a bug in Bridge.NET, see issue #2981.
		/// </summary>
		private static void Tokenize_NonAsciiCharacter_DoesNotThrow(Assert assert)
		{
			var tokenizer = new LatinWordTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This is—a test.").ToArray(),
				new[] {"This", "is", "—", "a", "test", "."});
		}
	}
}
