using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
	public class LinearRuleCascade<TData, TOffset> : RuleCascade<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		public LinearRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: base(rules)
		{
		}

		public LinearRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: base(rules, comparer)
		{
		}

		public LinearRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
			: base(rules, multiApp)
		{
		}

		public LinearRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp, IEqualityComparer<TData> comparer)
			: base(rules, multiApp, comparer)
		{
		}

		public override IEnumerable<TData> Apply(TData input)
		{
			var output = new HashSet<TData>(Comparer);
			ApplyRules(input, 0, output);
			return output;
		}

		private bool ApplyRules(TData input, int ruleIndex, HashSet<TData> output)
		{
			for (int i = ruleIndex; i < Rules.Count; i++)
			{
				bool applied = false;
				foreach (TData result in ApplyRule(Rules[i], i, input))
				{
					// avoid infinite loop
					if (!MultipleApplication || !Comparer.Equals(input, result))
					{
						if (ApplyRules(result, MultipleApplication ? i : i + 1, output))
							output.Add(result);
					}
					else
					{
						output.Add(result);
					}
					applied = true;
				}

				if (applied)
					return false;
			}
			return true;
		}
	}
}
