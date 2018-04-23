using Bridge.Html5;
using Bridge.QUnit;
using System.Linq;

namespace SIL.Machine.Tokenization
{
	public class LatinSentenceTokenizerTests
	{
		[Ready]
		public static void RunTests()
		{
			QUnit.Module(nameof(LatinSentenceTokenizerTests));

			QUnit.Test(nameof(Tokenize_Empty_ReturnsEmpty), Tokenize_Empty_ReturnsEmpty);
			QUnit.Test(nameof(Tokenize_SingleLine_ReturnsTokens), Tokenize_SingleLine_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_MultipleLines_ReturnsTokens), Tokenize_MultipleLines_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_TwoSentences_ReturnsTokens), Tokenize_TwoSentences_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_Quotes_ReturnsTokens), Tokenize_Quotes_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_QuotationInSentence_ReturnsTokens), Tokenize_QuotationInSentence_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_Parens_ReturnsTokens), Tokenize_Parens_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_Abbreviation_ReturnsTokens), Tokenize_Abbreviation_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_IncompleteSentence_ReturnsTokens), Tokenize_IncompleteSentence_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_CompleteSentenceWithSpaceAtEnd_ReturnsTokens),
				Tokenize_CompleteSentenceWithSpaceAtEnd_ReturnsTokens);
		}

		private static void Tokenize_Empty_ReturnsEmpty(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("").ToArray(), new string[0]);
		}

		private static void Tokenize_SingleLine_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This is a test.").ToArray(), new[] { "This is a test." });
		}

		private static void Tokenize_MultipleLines_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"This is the first sentence.\nThis is the second sentence.").ToArray(),
				new[] { "This is the first sentence.", "This is the second sentence." });
		}

		private static void Tokenize_TwoSentences_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"This is the first sentence. This is the second sentence.").ToArray(),
				new[] { "This is the first sentence.", "This is the second sentence." });
		}

		private static void Tokenize_Quotes_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"\"This is the first sentence.\" This is sentence two.").ToArray(),
				new[] { "\"This is the first sentence.\"", "This is sentence two." });
		}

		private static void Tokenize_QuotationInSentence_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"\"This is sentence one!\" he said. This is sentence two.").ToArray(),
				new[] { "\"This is sentence one!\" he said.", "This is sentence two." });
		}

		private static void Tokenize_Parens_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This is sentence one. (This is sentence two.)").ToArray(),
				new[] { "This is sentence one.", "(This is sentence two.)" });
		}

		private static void Tokenize_Abbreviation_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer(new[] { "mr", "dr", "ms" });
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"Mr. Smith went to Washington. This is sentence two.").ToArray(),
				new[] { "Mr. Smith went to Washington.", "This is sentence two." });
		}

		private static void Tokenize_IncompleteSentence_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This is an incomplete sentence ").ToArray(),
				new[] { "This is an incomplete sentence " });
		}

		private static void Tokenize_CompleteSentenceWithSpaceAtEnd_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LatinSentenceTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("\"This is a complete sentence.\" \n").ToArray(),
				new[] { "\"This is a complete sentence.\"" });
		}
	}
}
