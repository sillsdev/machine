using System.Collections.Generic;
using SIL.Machine.Annotations;
using System.Text.RegularExpressions;

namespace SIL.Machine.Tokenization
{
	public class LineSegmentTokenizer : WhitespaceTokenizer
	{
		private static readonly Regex NewLineRegex = new Regex("\n|\r\n?");

		public override IEnumerable<Range<int>> Tokenize(string data, Range<int> range)
		{
			int lineStart = 0;
			foreach (Match match in NewLineRegex.Matches(data.Substring(0, range.End), range.Start))
			{
				Range<int> segmentRange;
				if (TryGetSegmentRange(data, lineStart, match.Index + match.Length, false, out segmentRange))
					yield return segmentRange;
				lineStart = match.Index + match.Length;
			}

			if (lineStart < range.End)
			{
				Range<int> segmentRange;
				if (TryGetSegmentRange(data, lineStart, range.End, true, out segmentRange))
					yield return segmentRange;
			}
		}

		private bool TryGetSegmentRange(string data, int start, int end, bool last, out Range<int> segmentRange)
		{
			int segmentStart = -1, segmentEnd = -1;
			foreach (Range<int> charRange in base.Tokenize(data, Range<int>.Create(start, end)))
			{
				if (segmentStart == -1)
					segmentStart = charRange.Start;
				segmentEnd = charRange.End;
			}

			if (segmentStart != -1 && segmentEnd != -1)
			{
				segmentRange = Range<int>.Create(segmentStart, last ? end : segmentEnd);
				return true;
			}
			else if (!last)
			{
				segmentRange = Range<int>.Create(start, start);
				return true;
			}

			segmentRange = Range<int>.Null;
			return false;
		}
	}
}
