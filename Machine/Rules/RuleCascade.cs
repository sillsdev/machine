using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Rules
{
	public abstract class RuleCascade<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly ReadOnlyList<IRule<TData, TOffset>> _rules;
		private readonly bool _multiApp;
		private readonly IEqualityComparer<TData> _comparer;

		protected RuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
			: this(rules, false)
		{
		}

		protected RuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
			: this(rules, false, comparer)
		{
		}


		protected RuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
			: this(rules, multiApp, EqualityComparer<TData>.Default)
		{
		}

		protected RuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp, IEqualityComparer<TData> comparer)
		{
			_rules = new ReadOnlyList<IRule<TData, TOffset>>(rules.ToList());
			_multiApp = multiApp;
			_comparer = comparer;
		}

		public IEqualityComparer<TData> Comparer
		{
			get { return _comparer; }
		}

		public bool MultipleApplication
		{
			get { return _multiApp; }
		}

		public IReadOnlyList<IRule<TData, TOffset>> Rules
		{
			get { return _rules; }
		}

		public abstract IEnumerable<TData> Apply(TData input);

		protected virtual IEnumerable<TData> ApplyRule(IRule<TData, TOffset> rule, int index, TData input)
		{
			return rule.Apply(input);
		}
	}
}
