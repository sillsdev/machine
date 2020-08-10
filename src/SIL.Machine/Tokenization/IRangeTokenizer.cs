using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public interface IRangeTokenizer<TData, TOffset, TToken> : ITokenizer<TData, TOffset, TToken>
	{
		IEnumerable<Range<TOffset>> TokenizeAsRanges(TData data);
		IEnumerable<Range<TOffset>> TokenizeAsRanges(TData data, Range<TOffset> range);
	}
}
