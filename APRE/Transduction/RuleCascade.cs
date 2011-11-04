using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIL.APRE.Transduction
{
	public enum RuleCascadeOrder
	{
		Linear,
		Permutation,
		Combination
	}

	public abstract class RuleCascade<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly List<IRule<TData, TOffset>> _rules;

		protected RuleCascade()
		{
			_rules = new List<IRule<TData, TOffset>>();
		}

		public RuleCascadeOrder RuleCascadeOrder { get; set; }

		public bool MultipleApplication { get; set; }

		public ReadOnlyCollection<IRule<TData, TOffset>> Rules
		{
			get { return _rules.AsReadOnly(); }
		}

		public abstract bool IsApplicable(TData input);

		protected void InsertRuleInternal(int index, IRule<TData, TOffset> rule)
		{
			_rules.Insert(index, rule);
		}

		public virtual bool Apply(TData input, out IEnumerable<TData> output)
		{
			var outputList = new List<TData>();
			output = ApplyRules(input, RuleCascadeOrder == RuleCascadeOrder.Combination && !MultipleApplication ? new HashSet<int>() : null, 0, outputList) ? outputList : null;
			return output != null;
		}

		private bool ApplyRules(TData input, HashSet<int> rulesApplied, int ruleIndex, List<TData> output)
		{
			bool applied = false;
			for (int i = ruleIndex; i < _rules.Count; i++)
			{
				IEnumerable<TData> results;
				if ((rulesApplied == null || !rulesApplied.Contains(i)) && ApplyRule(_rules[i], input, out results))
				{
					foreach (TData result in results)
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

		protected virtual bool ApplyRule(IRule<TData, TOffset> rule, TData input, out IEnumerable<TData> output)
		{
			return rule.Apply(input, out output);
		}
	}
}
