using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class RegexTokenizer : ITokenizer<string, int>
	{
		private readonly SpanFactory<int> _spanFactory;
		private readonly Regex _regex;

		public RegexTokenizer(SpanFactory<int> spanFactory, string regexPattern)
		{
			_spanFactory = spanFactory;
			_regex = new Regex(regexPattern);
		}

		public IEnumerable<Span<int>> Tokenize(string data)
		{
			return _regex.Matches(data).Cast<Match>().Select(m => _spanFactory.Create(m.Index, m.Index + m.Length));
		}
	}
}
