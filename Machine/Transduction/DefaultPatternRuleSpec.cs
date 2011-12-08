using System;
using SIL.Machine.Matching;

namespace SIL.Machine.Transduction
{
	public delegate TOffset ApplyRhsDelegate<TData, TOffset>(PatternRule<TData, TOffset> rule, Match<TData, TOffset> match, out TData output) where TData : IData<TOffset>; 

	public class DefaultPatternRuleSpec<TData, TOffset> : IPatternRuleSpec<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly Pattern<TData, TOffset> _pattern; 
		private readonly ApplyRhsDelegate<TData, TOffset> _action;
		private readonly Func<TData, bool> _applicable;

		public DefaultPatternRuleSpec(Pattern<TData, TOffset> pattern, ApplyRhsDelegate<TData, TOffset> action)
			: this(pattern, action, ann => true)
		{
		}

		public DefaultPatternRuleSpec(Pattern<TData, TOffset> pattern, ApplyRhsDelegate<TData, TOffset> action, Func<TData, bool> applicable)
		{
			_pattern = pattern;
			_action = action;
			_applicable = applicable;
		}

		public Pattern<TData, TOffset> Pattern
		{
			get { return _pattern; }
		}

		public bool IsApplicable(TData input)
		{
			return _applicable(input);
		}

		public TOffset ApplyRhs(PatternRule<TData, TOffset> rule, Match<TData, TOffset> match, out TData output)
		{
			return _action(rule, match, out output);
		}
	}
}
