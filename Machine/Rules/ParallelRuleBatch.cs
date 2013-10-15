using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIL.Machine.Rules
{
	public class ParallelRuleBatch<TData, TOffset> : RuleBatch<TData, TOffset> where TData : IData<TOffset>
	{
		public ParallelRuleBatch(IEnumerable<IRule<TData, TOffset>> rules)
			: base(rules, false)
		{
		}

		public ParallelRuleBatch(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: base(rules, false, comparer)
		{
		}

		public override IEnumerable<TData> Apply(TData input)
		{
			var output = new ConcurrentStack<TData>();
			Parallel.ForEach(Rules, rule =>
				{
					TData[] outData = rule.Apply(input).ToArray();
					if (outData.Length > 0)
						output.PushRange(outData);
				});

			return output.Distinct(Comparer);
		}
	}
}
