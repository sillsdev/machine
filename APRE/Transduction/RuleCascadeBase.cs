using System.Collections.Generic;

namespace SIL.APRE.Transduction
{
	public abstract class RuleCascadeBase<TOffset> : IRule<TOffset>
	{
		private readonly List<IRule<TOffset>> _rules;
		private readonly bool _linear;

		protected RuleCascadeBase(IEnumerable<IRule<TOffset>> rules, bool linear)
		{
			_rules = new List<IRule<TOffset>>(rules);
			_linear = linear;
		}

		public abstract bool IsApplicable(IBidirList<Annotation<TOffset>> input);

		public virtual bool Apply(IBidirList<Annotation<TOffset>> input)
		{
			return ApplyRules(input, 0);
		}

		private bool ApplyRules(IBidirList<Annotation<TOffset>> input, int ruleIndex)
		{
			bool applied = false;
			for (int i = ruleIndex; i < _rules.Count; i++)
			{
				if (_rules[i].Apply(input))
				{
					ApplyRules(input, _linear ? i : 0);
					applied = true;
				}
			}
			return applied;
		}
	}
}
