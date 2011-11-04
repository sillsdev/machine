using System;
using System.Collections.Generic;

namespace SIL.APRE.Transduction
{
	public class DefaultRuleCascade<TData, TOffset> : RuleCascade<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly Func<TData, bool> _applicable;

		public DefaultRuleCascade()
			: this(input => true)
		{
		}

		public DefaultRuleCascade(Func<TData, bool> applicable)
		{
			_applicable = applicable;
		}

		public DefaultRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: this(rules, input => true)
		{
		}

		public DefaultRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, Func<TData, bool> applicable)
		{
			_applicable = applicable;
			foreach (IRule<TData, TOffset> rule in rules)
				AddRule(rule);
		}

		public override bool IsApplicable(TData input)
		{
			return _applicable(input);
		}

		public void AddRule(IRule<TData, TOffset> rule)
		{
			InsertRuleInternal(Rules.Count, rule);
		}

		public void InsertRule(int index, IRule<TData, TOffset> rule)
		{
			InsertRuleInternal(index, rule);
		}
	}
}
