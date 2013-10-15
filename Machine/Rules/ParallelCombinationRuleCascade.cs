using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIL.Machine.Rules
{
	public class ParallelCombinationRuleCascade<TData, TOffset> : CombinationRuleCascade<TData, TOffset> where TData : IData<TOffset>
	{
		public ParallelCombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: base(rules)
		{
		}

		public ParallelCombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: base(rules, comparer)
		{
		}

		public ParallelCombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
			: base(rules, multiApp)
		{
		}

		public ParallelCombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp, IEqualityComparer<TData> comparer)
			: base(rules, multiApp, comparer)
		{
		}

		public override IEnumerable<TData> Apply(TData input)
		{
			var output = new ConcurrentStack<TData>();
			var from = new ConcurrentStack<Tuple<TData, HashSet<int>>>();
			from.Push(Tuple.Create(input, !MultipleApplication ? new HashSet<int>() : null));
			var to = new ConcurrentStack<Tuple<TData, HashSet<int>>>();
			while (!from.IsEmpty)
			{
				to.Clear();
				Parallel.ForEach(from, work =>
					{
						for (int i = 0; i < Rules.Count; i++)
						{
							if ((work.Item2 == null || !work.Item2.Contains(i)))
							{
								TData[] results = ApplyRule(Rules[i], i, work.Item1).ToArray();
								if (results.Length > 0)
								{
									output.PushRange(results);

									Tuple<TData, HashSet<int>>[] workItems = results.Where(res => !Comparer.Equals(work.Item1, res))
										.Select(res => Tuple.Create(res, work.Item2 == null ? null : new HashSet<int>(work.Item2) {i})).ToArray();
									if (workItems.Length > 0)
										to.PushRange(workItems);
								}
							}
						}
					});
				ConcurrentStack<Tuple<TData, HashSet<int>>> temp = from;
				from = to;
				to = temp;
			}

			return output.Distinct(Comparer);
		}
	}
}
