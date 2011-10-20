using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public interface IPatternRuleAction<TData, TOffset> where TData : IData<TOffset>
	{
		bool IsApplicable(TData input);

		Annotation<TOffset> Apply(Pattern<TData, TOffset> lhs, TData input, PatternMatch<TOffset> match, out TData output);
	}
}
