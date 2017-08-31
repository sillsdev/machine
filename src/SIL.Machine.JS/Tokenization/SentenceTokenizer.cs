using System.Linq;

namespace SIL.Machine.Tokenization
{
	public class SentenceTokenizer
	{
		private readonly LatinSentenceTokenizer _tokenizer;

		public SentenceTokenizer(string[] abbreviations = null)
		{
			_tokenizer = new LatinSentenceTokenizer(abbreviations ?? new string[0]);
		}

		public Range[] Tokenize(string text)
		{
			return _tokenizer.Tokenize(text)
				.Select(s => new Range { Index = s.Start, Length = s.Length }).ToArray();
		}

		public string[] TokenizeToStrings(string text)
		{
			return _tokenizer.TokenizeToStrings(text).ToArray();
		}
	}
}
