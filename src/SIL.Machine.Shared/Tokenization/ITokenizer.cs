using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public interface ITokenizer<in TData, TOffset>
	{
		IEnumerable<Span<TOffset>> Tokenize(TData data);
		IEnumerable<Span<TOffset>> Tokenize(TData data, Span<TOffset> span);
	}
}
