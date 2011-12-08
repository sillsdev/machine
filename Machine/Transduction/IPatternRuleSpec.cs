using SIL.Machine.Matching;

namespace SIL.Machine.Transduction
{
	public interface IPatternRuleSpec<TData, TOffset> where TData : IData<TOffset>
	{
		Pattern<TData, TOffset> Pattern { get; }

		bool IsApplicable(TData input);

		TOffset ApplyRhs(PatternRule<TData, TOffset> rule, Match<TData, TOffset> match, out TData output);
	}
}
