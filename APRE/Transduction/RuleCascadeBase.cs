using System.Collections.Generic;
using System.Linq;

namespace SIL.APRE.Transduction
{
	public enum RuleCascadeOrder
	{
		Linear,
		Permutation,
		Combination
	}

	public abstract class RuleCascadeBase<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly List<IRule<TData, TOffset>> _rules;
		private readonly RuleCascadeOrder _ruleCascadeOrder;

		protected RuleCascadeBase(RuleCascadeOrder ruleCascadeOrder)
			: this(ruleCascadeOrder, Enumerable.Empty<IRule<TData, TOffset>>())
		{
		}

		protected RuleCascadeBase(RuleCascadeOrder ruleCascadeOrder, IEnumerable<IRule<TData, TOffset>> rules)
		{
			_ruleCascadeOrder = ruleCascadeOrder;
			_rules = new List<IRule<TData, TOffset>>(rules);
		}

		public RuleCascadeOrder RuleCascadeOrder
		{
			get { return _ruleCascadeOrder; }
		}

		public IEnumerable<IRule<TData, TOffset>> Rules
		{
			get { return _rules; }
		}

		public abstract bool IsApplicable(TData input);

		protected void AddRuleInternal(IRule<TData, TOffset> rule, bool end)
		{
			_rules.Insert(end ? _rules.Count : 0, rule);
		}

		public virtual bool Apply(TData input, out IEnumerable<TData> output)
		{
			var outputList = new List<TData>();
			output = ApplyRules(input, 0, outputList) ? outputList : null;
			return output != null;
		}

		private bool ApplyRules(TData input, int ruleIndex, List<TData> output)
		{
			bool applied = false;
			for (int i = ruleIndex; i < _rules.Count; i++)
			{
				IEnumerable<TData> results;
				if (ApplyRule(_rules[i], input, out results))
				{
					foreach (TData result in results)
					{
						switch (_ruleCascadeOrder)
						{
							case RuleCascadeOrder.Permutation:
								if (!ApplyRules(result, i, output))
									output.Add(result);
								break;

							case RuleCascadeOrder.Combination:
								if (!ApplyRules(result, 0, output))
									output.Add(result);
								break;

							case RuleCascadeOrder.Linear:
								if (!ApplyRules(result, i + 1, output))
									output.Add(result);
								break;
						}
					}

					applied = true;
					if (_ruleCascadeOrder == RuleCascadeOrder.Linear)
						break;
				}
			}

			return applied;
		}

		protected virtual bool ApplyRule(IRule<TData, TOffset> rule, TData input, out IEnumerable<TData> output)
		{
			return rule.Apply(input, out output);
		}
	}
}
