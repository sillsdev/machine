using System.Collections.Generic;

namespace SIL.Machine.Rules
{
	public class PermutationRuleCascade<TData, TOffset> : RuleCascade<TData, TOffset> where TData : IData<TOffset>
	{
		public PermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: base(rules)
		{
		}

		public PermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: base(rules, comparer)
		{
		}

		public PermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
			: base(rules, multiApp)
		{
		}

		public PermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp, IEqualityComparer<TData> comparer)
			: base(rules, multiApp, comparer)
		{
		}

		public override IEnumerable<TData> Apply(TData input)
		{
			var output = new HashSet<TData>(Comparer);
			ApplyRules(input, 0, output);
			return output;
		}

		private void ApplyRules(TData input, int ruleIndex, HashSet<TData> output)
		{
			for (int i = ruleIndex; i < Rules.Count; i++)
			{
				foreach (TData result in ApplyRule(Rules[i], i, input))
				{
					// avoid infinite loop
					if (!MultipleApplication || !Comparer.Equals(input, result))
						ApplyRules(result, MultipleApplication ? i : i + 1, output);
					output.Add(result);
				}
			}
		}
	}
}
