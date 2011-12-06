using SIL.Machine.Matching;

namespace SIL.Machine.Transduction
{
	public class NullPatternRuleAction<TData, TOffset> : IPatternRuleAction<TData, TOffset> where TData : IData<TOffset>
	{
		public bool IsApplicable(TData input)
		{
			return true;
		}

		public Annotation<TOffset> Apply(PatternRule<TData, TOffset> rule, TData input, PatternMatch<TOffset> match, out TData output)
		{
			output = default(TData);
			return null;
		}
	}
}
