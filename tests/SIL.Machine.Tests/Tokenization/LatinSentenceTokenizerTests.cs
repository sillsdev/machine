using System;
using System.Collections.Generic;
using System.Text;
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
			Assert.That(tokenizer.TokenizeToStrings("This is a test."), Is.EqualTo(new[] {"This is a test."}));
		}

		[Test]
		public void Tokenize_MultipleLines_ReturnsTokens()
		{
			var tokenizer = new LatinSentenceTokenizer();
			Assert.That(tokenizer.TokenizeToStrings("This is the first sentence.\nThis is the second sentence."),
				Is.EqualTo(new[] {"This is the first sentence.", "This is the second sentence."}));
		}
	}
}
