using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.Rules
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

		public IReadOnlyList<IRule<TData, TOffset>> Rules
		{
			get { return _rules.AsReadOnlyList(); }
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
					if (_rules[i].IsApplicable(input))
					{
						foreach (TData result in ApplyRule(_rules[i], i, input))
						{
							switch (RuleCascadeOrder)
							{
								case RuleCascadeOrder.Linear:
									if (!ApplyRules(result, null, MultipleApplication ? i : i + 1, output))
										output.Add(result);
									break;

								case RuleCascadeOrder.Permutation:
									ApplyRules(result, null, MultipleApplication ? i : i + 1, output);
									output.Add(result);
									break;

								case RuleCascadeOrder.Combination:
									ApplyRules(result, rulesApplied == null ? null : new HashSet<int>(rulesApplied){i}, 0, output);
									output.Add(result);
									break;
							}
							applied = true;
						}

						if (!Continue(_rules[i], i, input) || (applied && RuleCascadeOrder == RuleCascadeOrder.Linear))
							break;
					}
				}
			}

			return applied;
		}

		protected virtual IEnumerable<TData> ApplyRule(IRule<TData, TOffset> rule, int index, TData input)
		{
			return rule.Apply(input);
		}

		protected virtual bool Continue(IRule<TData, TOffset> rule, int index, TData input)
		{
			return true;
		}
	}
}
