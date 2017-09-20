using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class WhitespaceTokenizer : StringTokenizer
	{
		public override IEnumerable<Span<int>> Tokenize(string data, Span<int> span)
		{
			int startIndex = -1;
			for (int i = span.Start; i < span.End; i++)
			{
				if (char.IsWhiteSpace(data[i]))
				{
					if (startIndex != -1)
						yield return Span<int>.Create(startIndex, i);
					startIndex = -1;
				}
				else if (startIndex == -1)
				{
					startIndex = i;
				}
			}

			if (startIndex != -1)
				yield return Span<int>.Create(startIndex, data.Length);
		}
	}
}
