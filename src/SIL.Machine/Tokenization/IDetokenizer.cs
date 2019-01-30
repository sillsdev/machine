using System.Collections.Generic;

namespace SIL.Machine.Tokenization
{
	public interface IDetokenizer<out TData, in TToken>
	{
		TData Detokenize(IEnumerable<TToken> tokens);
	}
}
