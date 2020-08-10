using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public interface ITokenizer<TData, TOffset, TToken>
	{
		IEnumerable<TToken> Tokenize(TData data);
		IEnumerable<TToken> Tokenize(TData data, Range<TOffset> range);
	}
}
