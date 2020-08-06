using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class WhitespaceTokenizer : StringTokenizer
	{
		public override IEnumerable<Range<int>> Tokenize(string data, Range<int> range)
		{
			int startIndex = -1;
			for (int i = range.Start; i < range.End; i++)
			{
				if (IsWhiteSpace(data[i]))
				{
					if (startIndex != -1)
						yield return Range<int>.Create(startIndex, i);
					startIndex = -1;
				}
				else if (startIndex == -1)
				{
					startIndex = i;
				}
			}

			if (startIndex != -1)
				yield return Range<int>.Create(startIndex, range.End);
		}

		protected virtual bool IsWhiteSpace(char c)
		{
			return char.IsWhiteSpace(c) || c == '\u200b' || c == '\ufeff';
		}
	}
}
