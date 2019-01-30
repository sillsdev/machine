using SIL.Machine.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Tokenization
{
	public static class TokenizationExtensions
	{
		public static IEnumerable<string> TokenizeToStrings(this ITokenizer<string, int> tokenizer, string str)
		{
			return tokenizer.TokenizeToStrings(str, Range<int>.Create(0, str.Length));
		}

		public static IEnumerable<string> TokenizeToStrings(this ITokenizer<string, int> tokenizer, string str,
			Range<int> range)
		{
			return tokenizer.Tokenize(str, range).Select(r => str.Substring(r.Start, r.Length));
		}
	}
}
