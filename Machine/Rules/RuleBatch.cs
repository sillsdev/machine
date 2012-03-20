using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Rules
{
	public class RuleBatch<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly List<IRule<TData, TOffset>> _rules;

		public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules)
		{
			_rules = new List<IRule<TData, TOffset>>(rules);
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
			foreach (IRule<TData, TOffset> rule in _rules)
			{
				if (rule.IsApplicable(input))
				{
					TData[] output = rule.Apply(input).ToArray();
					if (output.Length > 0)
						return output;
				}
			}

			return Enumerable.Empty<TData>();
		}
	}
}
