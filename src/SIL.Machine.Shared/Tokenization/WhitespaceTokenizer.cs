using System.Collections.Generic;
using SIL.Machine.Annotations;
using System.Linq;

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

		public IEnumerable<Span<int>> Tokenize(string data)
		{
			if (data == "")
				return Enumerable.Empty<Span<int>>();

			return Tokenize(data, SpanFactory.Create(0, data.Length));
		}

		public virtual IEnumerable<Span<int>> Tokenize(string data, Span<int> dataSpan)
		{
			if (dataSpan.IsEmpty)
				yield break;

			int startIndex = -1;
			for (int i = dataSpan.Start; i < dataSpan.End; i++)
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
