using System;
using System.Collections.Generic;

namespace SIL.APRE.Transduction
{
	public class RuleCascade<TOffset> : RuleCascadeBase<TOffset>
	{
		private readonly Func<IBidirList<Annotation<TOffset>>, bool> _applicable;

		public RuleCascade(RuleOrder ruleOrder)
			: this(ruleOrder, ann => true)
		{
		}

		public RuleCascade(RuleOrder ruleOrder, Func<IBidirList<Annotation<TOffset>>, bool> applicable)
			: base(ruleOrder)
		{
			_applicable = applicable;
		}

		public RuleCascade(RuleOrder ruleOrder, IEnumerable<IRule<TOffset>> rules)
			: this(ruleOrder, rules, ann => true)
		{
		}

		public RuleCascade(RuleOrder ruleOrder, IEnumerable<IRule<TOffset>> rules, Func<IBidirList<Annotation<TOffset>>, bool> applicable)
			: base(ruleOrder, rules)
		{
			_applicable = applicable;
		}

		public override bool IsApplicable(IBidirList<Annotation<TOffset>> input)
		{
			return _applicable(input);
		}

		public void AddRule(IRule<TOffset> rule)
		{
			AddRuleInternal(rule);
		}
	}
}
