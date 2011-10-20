using System;
using System.Collections.Generic;

namespace SIL.APRE.Transduction
{
	public class RuleCascade<TData, TOffset> : RuleCascadeBase<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly Func<TData, bool> _applicable;

		public RuleCascade(RuleCascadeOrder ruleCascadeOrder)
			: this(ruleCascadeOrder, input => true)
		{
		}

		public RuleCascade(RuleCascadeOrder ruleCascadeOrder, Func<TData, bool> applicable)
			: base(ruleCascadeOrder)
		{
			_applicable = applicable;
		}

		public RuleCascade(RuleCascadeOrder ruleCascadeOrder, IEnumerable<IRule<TData, TOffset>> rules)
			: this(ruleCascadeOrder, rules, input => true)
		{
		}

		public RuleCascade(RuleCascadeOrder ruleCascadeOrder, IEnumerable<IRule<TData, TOffset>> rules, Func<TData, bool> applicable)
			: base(ruleCascadeOrder, rules)
		{
			_applicable = applicable;
		}

		public override bool IsApplicable(TData input)
		{
			return _applicable(input);
		}

		public void AddRule(IRule<TData, TOffset> rule)
		{
			AddRuleInternal(rule, true);
		}
	}
}
