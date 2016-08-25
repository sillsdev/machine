using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class WhitespaceTokenizer : ITokenizer<string, int>
	{
		private readonly SpanFactory<int> _spanFactory;

		public WhitespaceTokenizer(SpanFactory<int> spanFactory)
		{
			_spanFactory = spanFactory;
		}

		public IEnumerable<Span<int>> Tokenize(string data)
		{
			int startIndex = -1;
			for (int i = 0; i < data.Length; i++)
			{
				if (char.IsWhiteSpace(data[i]))
				{
					if (startIndex != -1)
						yield return _spanFactory.Create(startIndex, i);
					startIndex = -1;
				}
				else if (startIndex == -1)
				{
					startIndex = i;
				}
			}
		}
	}
}
