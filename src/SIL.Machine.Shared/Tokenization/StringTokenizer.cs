using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public abstract class StringTokenizer : ITokenizer<string, int>
	{
		public IEnumerable<Span<int>> Tokenize(string data)
		{
			return Tokenize(data, Span<int>.Create(0, data.Length));
		}

		public abstract IEnumerable<Span<int>> Tokenize(string data, Span<int> dataSpan);
	}
}
