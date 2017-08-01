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

		public RegexTokenizer(string regexPattern)
			: this (new IntegerSpanFactory(), regexPattern)
		{
		}

		public RegexTokenizer(SpanFactory<int> spanFactory, string regexPattern)
		{
			_spanFactory = spanFactory;
			_regex = new Regex(regexPattern);
		}

		public IEnumerable<Span<int>> Tokenize(string data)
		{
			return Tokenize(data, data.Length == 0 ? _spanFactory.Empty : _spanFactory.Create(0, data.Length));
		}

		public IEnumerable<Span<int>> Tokenize(string data, Span<int> dataSpan)
		{
			if (dataSpan.IsEmpty)
				return Enumerable.Empty<Span<int>>();

			return _regex.Matches(data.Substring(0, dataSpan.End), dataSpan.Start).Cast<Match>()
				.Select(m => _spanFactory.Create(m.Index, m.Index + m.Length));
		}
	}
}
