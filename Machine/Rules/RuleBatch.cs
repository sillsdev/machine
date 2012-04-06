using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Rules
{
	public class RuleBatch<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly List<IRule<TData, TOffset>> _rules;
		private readonly bool _permutation;

		public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules)
			: this(rules, false)
		{
		}

		public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules, bool permutation)
		{
			_rules = new List<IRule<TData, TOffset>>(rules);
			_permutation = permutation;
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
			var output = new List<TData>();
			foreach (IRule<TData, TOffset> rule in _rules)
			{
				if (rule.IsApplicable(input))
				{
					output.AddRange(rule.Apply(input));
					if (!_permutation && output.Count > 0)
						return output;
				}
			}

			return output;
		}
	}
}
