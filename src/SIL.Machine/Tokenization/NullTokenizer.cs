using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class NullTokenizer : StringTokenizer
	{
		public override IEnumerable<Range<int>> Tokenize(string data, Range<int> range)
		{
			yield return range;
		}
	}
}
