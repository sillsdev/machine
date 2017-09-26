using System.Collections.Generic;
using SIL.Machine.Annotations;
using System.Text.RegularExpressions;

namespace SIL.Machine.Tokenization
{
	public class LineSegmentTokenizer : StringTokenizer
	{
		private static readonly Regex NewLineRegex = new Regex("\n|\r\n?");

		public override IEnumerable<Range<int>> Tokenize(string data, Range<int> range)
		{
			int lineStart = 0;
			foreach (Match match in NewLineRegex.Matches(data.Substring(0, range.End), range.Start))
			{
				yield return Range<int>.Create(lineStart, match.Index);
				lineStart = match.Index + match.Length;
			}

			if (lineStart < range.End)
				yield return Range<int>.Create(lineStart, range.End);
		}
	}
}
