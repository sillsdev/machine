using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public interface ITokenizer<in TData, TOffset>
	{
		IEnumerable<Range<TOffset>> Tokenize(TData data);
		IEnumerable<Range<TOffset>> Tokenize(TData data, Range<TOffset> range);
	}
}
