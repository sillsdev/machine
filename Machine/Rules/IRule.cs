using System.Collections.Generic;

namespace SIL.Machine.Rules
{
	public interface IRule<TData, TOffset> where TData : IData<TOffset>
	{
		IEnumerable<TData> Apply(TData input);
	}
}
