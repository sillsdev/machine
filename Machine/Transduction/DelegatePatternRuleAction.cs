using System;
using SIL.Machine.Matching;

namespace SIL.Machine.Transduction
{
	public delegate Annotation<TOffset> ApplyDelegate<TData, TOffset>(PatternRule<TData, TOffset> rule, TData input, PatternMatch<TOffset> match, out TData output) where TData : IData<TOffset>; 

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

		public Annotation<TOffset> Apply(PatternRule<TData, TOffset> rule, TData input, PatternMatch<TOffset> match, out TData output)
		{
			return _action(rule, input, match, out output);
		}
	}
}
