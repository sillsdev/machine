using System;
using SIL.Collections;

namespace SIL.HermitCrab
{
	public class TraceRuleCollection : IDBearerSet<IHCRule>
	{
		private readonly IDBearerSet<IHCRule> _rules;

		internal TraceRuleCollection(IDBearerSet<IHCRule> rules)
		{
			_rules = rules;
		}

		public bool Add(string id)
		{
			IHCRule rule;
			if (!_rules.TryGetValue(id, out rule))
				throw new ArgumentException("The specified rule is not valid.", "id");
			return base.Add(rule);
		}

		public void AddAllRules()
		{
			UnionWith(_rules);
		}

		public bool IsTracingAllRules
		{
			get { return Count == _rules.Count; }
		}
	}
}
