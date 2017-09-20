using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class RegexTokenizer : StringTokenizer
	{
		private readonly Regex _regex;

		public RegexTokenizer(string regexPattern)
		{
			_regex = new Regex(regexPattern);
		}

		public override IEnumerable<Span<int>> Tokenize(string data, Span<int> dataSpan)
		{
			return _regex.Matches(data.Substring(0, dataSpan.End), dataSpan.Start).Cast<Match>()
				.Select(m => Span<int>.Create(m.Index, m.Index + m.Length));
		}
	}
}
