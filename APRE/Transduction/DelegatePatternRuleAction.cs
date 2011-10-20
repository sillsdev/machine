using System;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public delegate Annotation<TOffset> ApplyDelegate<TData, TOffset>(Pattern<TData, TOffset> lhs, TData input, PatternMatch<TOffset> match, out TData output) where TData : IData<TOffset>; 

	public class DelegatePatternRuleAction<TData, TOffset> : IPatternRuleAction<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly ApplyDelegate<TData, TOffset> _action;
		private readonly Func<TData, bool> _applicable;

		public DelegatePatternRuleAction(ApplyDelegate<TData, TOffset> action)
			: this(action, ann => true)
		{
		}

		public DelegatePatternRuleAction(ApplyDelegate<TData, TOffset> action, Func<TData, bool> applicable)
		{
			_action = action;
			_applicable = applicable;
		}

		public bool IsApplicable(TData input)
		{
			return _applicable(input);
		}

		public Annotation<TOffset> Apply(Pattern<TData, TOffset> lhs, TData input, PatternMatch<TOffset> match, out TData output)
		{
			return _action(lhs, input, match, out output);
		}
	}
}
