using SIL.Collections;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules
{
	public interface IPatternRuleSpec<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		Pattern<TData, TOffset> Pattern { get; }

		bool IsApplicable(TData input);

		TOffset ApplyRhs(PatternRule<TData, TOffset> rule, Match<TData, TOffset> match, out TData output);
	}
}
