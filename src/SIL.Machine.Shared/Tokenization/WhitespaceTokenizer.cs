using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class WhitespaceTokenizer : ITokenizer<string, int>
	{
		public WhitespaceTokenizer()
			: this(new IntegerSpanFactory())
		{
		}

		public WhitespaceTokenizer(SpanFactory<int> spanFactory)
		{
			SpanFactory = spanFactory;
		}

		protected SpanFactory<int> SpanFactory { get; }

		public virtual IEnumerable<Span<int>> Tokenize(string data)
		{
			int startIndex = -1;
			for (int i = 0; i < data.Length; i++)
			{
				if (char.IsWhiteSpace(data[i]))
				{
					if (startIndex != -1)
						yield return SpanFactory.Create(startIndex, i);
					startIndex = -1;
				}
				else if (startIndex == -1)
				{
					startIndex = i;
				}
			}

			if (startIndex != -1)
				yield return SpanFactory.Create(startIndex, data.Length);
		}
	}
}
