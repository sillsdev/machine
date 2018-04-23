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

		public override IEnumerable<Range<int>> Tokenize(string data, Range<int> range)
		{
			return _regex.Matches(data.Substring(0, range.End), range.Start).Cast<Match>()
				.Select(m => Range<int>.Create(m.Index, m.Index + m.Length));
		}
	}
}
