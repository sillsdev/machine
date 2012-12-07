using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIL.Machine.Rules
{
	public class MultithreadedPermutationRuleCascade<TData, TOffset> : PermutationRuleCascade<TData, TOffset> where TData : IData<TOffset>
	{
		public MultithreadedPermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: base(rules)
		{
		}

		public MultithreadedPermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: base(rules, comparer)
		{
		}

		public MultithreadedPermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
			: base(rules, multiApp)
		{
		}

		public MultithreadedPermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp, IEqualityComparer<TData> comparer)
			: base(rules, multiApp, comparer)
		{
		}

		public override IEnumerable<TData> Apply(TData input)
		{
			return ApplyRules(input, 0);
		}

		private HashSet<TData> ApplyRules(TData input, int ruleIndex)
		{
			var tasks = new List<Task>();
			var output = new HashSet<TData>(Comparer);
			for (int i = ruleIndex; i < Rules.Count; i++)
			{
				foreach (TData result in ApplyRule(Rules[i], i, input))
				{
					// avoid infinite loop
					if (!MultipleApplication || !Comparer.Equals(input, result))
					{
						TData res = result;
						int nextRuleIndex = MultipleApplication ? i : i + 1;
						tasks.Add(Task.Factory.StartNew(() =>
							{
								HashSet<TData> results = ApplyRules(res, nextRuleIndex);
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

			Task.WaitAll(tasks.ToArray());
			return output;
		}
	}
}
