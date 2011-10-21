using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public class NullPatternRuleAction<TData, TOffset> : IPatternRuleAction<TData, TOffset> where TData : IData<TOffset>
	{
		public bool IsApplicable(TData input)
		{
			return true;
		}

		public Annotation<TOffset> Apply(TData input, PatternMatch<TOffset> match, out TData output)
		{
			output = default(TData);
			return null;
		}
	}
}
