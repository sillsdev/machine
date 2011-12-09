using System.Collections.Generic;

namespace SIL.Machine.Transduction
{
	public enum RuleCascadeOrder
	{
		Linear,
		Permutation,
		Combination
	}

	public class RuleCascade<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly List<IRule<TData, TOffset>> _rules;
		private readonly RuleCascadeOrder _order;
		private readonly bool _multiApp;

		public RuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: this(rules, RuleCascadeOrder.Linear)
		{
		}

		public RuleCascade(IEnumerable<IRule<TData, TOffset>> rules, RuleCascadeOrder order)
			: this(rules, order, false)
		{
		}

		public RuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
			: this(rules, RuleCascadeOrder.Linear, multiApp)
		{
		}

		public RuleCascade(IEnumerable<IRule<TData, TOffset>> rules, RuleCascadeOrder order, bool multiApp)
		{
			_rules = new List<IRule<TData, TOffset>>(rules);
			_order = order;
			_multiApp = multiApp;
		}

		public RuleCascadeOrder RuleCascadeOrder
		{
			get { return _order; }
		}

		public bool MultipleApplication
		{
			get { return _multiApp; }
		}

		public IEnumerable<IRule<TData, TOffset>> Rules
		{
			get { return _rules; }
		}

		public virtual bool IsApplicable(TData input)
		{
			return true;
		}

		public virtual IEnumerable<TData> Apply(TData input)
		{
			var outputList = new List<TData>();
			ApplyRules(input, RuleCascadeOrder == RuleCascadeOrder.Combination && !MultipleApplication ? new HashSet<int>() : null, 0, outputList);
			return outputList;
		}

		private bool ApplyRules(TData input, HashSet<int> rulesApplied, int ruleIndex, List<TData> output)
		{
			bool applied = false;
			for (int i = ruleIndex; i < _rules.Count; i++)
			{
				if ((rulesApplied == null || !rulesApplied.Contains(i)))
				{
					foreach (TData result in ApplyRule(_rules[i], input))
					{
						switch (RuleCascadeOrder)
						{
							case RuleCascadeOrder.Linear:
							case RuleCascadeOrder.Permutation:
								if (!ApplyRules(result, null, MultipleApplication ? i : i + 1, output))
									output.Add(result);
								break;

							case RuleCascadeOrder.Combination:
								if (!ApplyRules(result, rulesApplied == null ? null : new HashSet<int>(rulesApplied) {i}, 0, output))
									output.Add(result);
								break;
						}
					}

					applied = true;
					if (RuleCascadeOrder == RuleCascadeOrder.Linear)
						break;
				}
			}

			return applied;
		}

		protected virtual IEnumerable<TData> ApplyRule(IRule<TData, TOffset> rule, TData input)
		{
			return rule.Apply(input);
		}
	}
}
