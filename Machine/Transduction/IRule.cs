using System.Collections.Generic;

namespace SIL.Machine.Transduction
{
	public interface IRule<TData, TOffset> where TData : IData<TOffset>
	{
		bool IsApplicable(TData input);

		IEnumerable<TData> Apply(TData input);
	}
}
