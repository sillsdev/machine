using System.Collections.Generic;

namespace SIL.APRE.Rules
{
	public class RuleCascade<TOffset>
	{
		private readonly List<Rule<TOffset>> _rules;
		private readonly bool _linear;

		public RuleCascade(IEnumerable<Rule<TOffset>> rules, bool linear)
		{
			_rules = new List<Rule<TOffset>>(rules);
			_linear = linear;
		}

		public void Apply(IBidirList<Annotation<TOffset>> input)
		{
			ApplyRules(input, 0);
		}

		private void ApplyRules(IBidirList<Annotation<TOffset>> input, int ruleIndex)
		{
			for (int i = ruleIndex; i < _rules.Count; i++)
			{
				if (_rules[i].Apply(input))
					ApplyRules(input, _linear ? i : 0);
			}
		}
	}
}
