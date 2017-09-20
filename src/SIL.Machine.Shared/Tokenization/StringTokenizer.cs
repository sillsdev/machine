using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public abstract class StringTokenizer : ITokenizer<string, int>
	{
		public IEnumerable<Range<int>> Tokenize(string data)
		{
			return Tokenize(data, Range<int>.Create(0, data.Length));
		}

		public abstract IEnumerable<Range<int>> Tokenize(string data, Range<int> range);
	}
}
