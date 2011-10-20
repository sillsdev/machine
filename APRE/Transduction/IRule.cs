using System.Collections.Generic;

namespace SIL.APRE.Transduction
{
	public interface IRule<TData, TOffset> where TData : IData<TOffset>
	{
		bool IsApplicable(TData input);

		bool Apply(TData input, out IEnumerable<TData> output);
	}
}
