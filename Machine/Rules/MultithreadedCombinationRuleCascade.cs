using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIL.Machine.Rules
{
	public class MultithreadedCombinationRuleCascade<TData, TOffset> : CombinationRuleCascade<TData, TOffset> where TData : IData<TOffset>
	{
		public MultithreadedCombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: base(rules)
		{
		}

		public MultithreadedCombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: base(rules, comparer)
		{
		}

		public MultithreadedCombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
			: base(rules, multiApp)
		{
		}

		public MultithreadedCombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp, IEqualityComparer<TData> comparer)
			: base(rules, multiApp, comparer)
		{
		}

		public override IEnumerable<TData> Apply(TData input)
		{
			return ApplyRules(input, !MultipleApplication ? new HashSet<int>() : null, 0);
		}

		private HashSet<TData> ApplyRules(TData input, HashSet<int> rulesApplied, int ruleIndex)
		{
			var tasks = new List<Task>();
			var output = new HashSet<TData>(Comparer);
			for (int i = ruleIndex; i < Rules.Count; i++)
			{
				if ((rulesApplied == null || !rulesApplied.Contains(i)))
				{
					foreach (TData result in ApplyRule(Rules[i], i, input))
					{
						// avoid infinite loop
						if (!Comparer.Equals(input, result))
						{
							TData res = result;
							int curRuleIndex = i;
							tasks.Add(Task.Factory.StartNew(() =>
								{
									HashSet<TData> results = ApplyRules(res, rulesApplied == null ? null : new HashSet<int>(rulesApplied) {curRuleIndex}, 0);
									if (results.Count > 0)
									{
										lock (output)
											output.UnionWith(results);
									}
								}));
						}
						lock (output)
							output.Add(result);
					}
				}
			}

			Task.WaitAll(tasks.ToArray());
			return output;
		}
	}
}
