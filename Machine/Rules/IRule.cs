using System.Collections.Generic;

namespace SIL.Machine.Rules
{
	public interface IRule<TData, TOffset> where TData : IData<TOffset>
	{
		bool IsApplicable(TData input);

		IEnumerable<TData> Apply(TData input);
	}
}
