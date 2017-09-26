using Bridge.Html5;
using Bridge.QUnit;
using System.Linq;

namespace SIL.Machine.Tokenization
{
	public class LineSegmentTokenizerTests
	{
		[Ready]
		public static void RunTests()
		{
			QUnit.Module(nameof(LineSegmentTokenizerTests));

			QUnit.Test(nameof(Tokenize_Empty_ReturnsEmpty), Tokenize_Empty_ReturnsEmpty);
			QUnit.Test(nameof(Tokenize_SingleLine_ReturnsTokens), Tokenize_SingleLine_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_MultipleLines_ReturnsTokens), Tokenize_MultipleLines_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_EndsWithNewLine_ReturnsTokens), Tokenize_EndsWithNewLine_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_EndsWithNewLineAndSpace_ReturnsTokens),
				Tokenize_EndsWithNewLineAndSpace_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_EndsWithTextAndSpace_ReturnsTokens),
				Tokenize_EndsWithTextAndSpace_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_EmptyLine_ReturnsTokens), Tokenize_EmptyLine_ReturnsTokens);
			QUnit.Test(nameof(Tokenize_LineEndsWithSpace_ReturnsTokens), Tokenize_LineEndsWithSpace_ReturnsTokens);
		}

		private static void Tokenize_Empty_ReturnsEmpty(Assert assert)
		{
			var tokenizer = new LineSegmentTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("").ToArray(), new string[0]);
		}

		private static void Tokenize_SingleLine_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LineSegmentTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This is a test.").ToArray(), new[] { "This is a test." });
		}

		private static void Tokenize_MultipleLines_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LineSegmentTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"This is the first sentence.\nThis is the second sentence.").ToArray(),
				new[] { "This is the first sentence.", "This is the second sentence." });
		}

		private static void Tokenize_EndsWithNewLine_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LineSegmentTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This is a test.\n").ToArray(), new[] { "This is a test." });
		}

		private static void Tokenize_EndsWithNewLineAndSpace_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LineSegmentTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings("This is a test.\n ").ToArray(),
				new[] { "This is a test.", " " });
		}

		private static void Tokenize_EndsWithTextAndSpace_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LineSegmentTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"This is the first sentence.\nThis is a partial sentence ").ToArray(),
				new[] { "This is the first sentence.", "This is a partial sentence " });
		}

		private static void Tokenize_EmptyLine_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LineSegmentTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"This is the first sentence.\n\nThis is the third sentence.").ToArray(),
				new[] { "This is the first sentence.", "", "This is the third sentence." });
		}

		private static void Tokenize_LineEndsWithSpace_ReturnsTokens(Assert assert)
		{
			var tokenizer = new LineSegmentTokenizer();
			assert.DeepEqual(tokenizer.TokenizeToStrings(
				"This is the first sentence. \nThis is the second sentence.").ToArray(),
				new[] { "This is the first sentence. ", "This is the second sentence." });
		}
	}
}
