using System.Collections.Generic;

namespace SIL.Machine.Rules
{
	public class CombinationRuleCascade<TData, TOffset> : RuleCascade<TData, TOffset> where TData : IData<TOffset>
	{
		public CombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: base(rules)
		{
		}

		public CombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: base(rules, comparer)
		{
		}

		public CombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
			: base(rules, multiApp)
		{
		}

		public CombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp, IEqualityComparer<TData> comparer)
			: base(rules, multiApp, comparer)
		{
		}

		public override IEnumerable<TData> Apply(TData input)
		{
			var output = new HashSet<TData>(Comparer);
			ApplyRules(input, !MultipleApplication ? new HashSet<int>() : null, output);
			return output;
		}

		private void ApplyRules(TData input, HashSet<int> rulesApplied, HashSet<TData> output)
		{
			for (int i = 0; i < Rules.Count; i++)
			{
				if ((rulesApplied == null || !rulesApplied.Contains(i)))
				{
					foreach (TData result in ApplyRule(Rules[i], i, input))
					{
						// avoid infinite loop
						if (!Comparer.Equals(input, result))
							ApplyRules(result, rulesApplied == null ? null : new HashSet<int>(rulesApplied) {i}, output);
						output.Add(result);
					}
				}
			}
		}
	}
}
