using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public abstract class PatternRuleBatch<TData, TOffset> : PatternRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly List<PatternRule<TData, TOffset>> _rules; 
		private readonly Dictionary<string, PatternRule<TData, TOffset>> _ruleIds;

		protected PatternRuleBatch(SpanFactory<TOffset> spanFactory)
			: this(new Pattern<TData, TOffset>(spanFactory))
		{
		}

		protected PatternRuleBatch(Pattern<TData, TOffset> pattern)
			: base(pattern)
		{
			_rules = new List<PatternRule<TData, TOffset>>();
			_ruleIds = new Dictionary<string, PatternRule<TData, TOffset>>();
		}

		public ReadOnlyCollection<PatternRule<TData, TOffset>> Rules
		{
			get { return _rules.AsReadOnly(); }
		}

		protected void InsertRuleInternal(int index, PatternRule<TData, TOffset> rule)
		{
			string id = "rule" + _rules.Count;
			_rules.Insert(index, rule);
			_ruleIds[id] = rule;
			var expr = new Expression<TData, TOffset>(id, rule.Lhs.Children.Clone())
			           	{
			           		Acceptable = (input, match) => rule.IsApplicable(input) && rule.Lhs.Acceptable(input, match)
			           	};

			Lhs.Children.Insert(expr, index == _rules.Count - 1 ? Lhs.Children.Last : Lhs.Children.ElementAtOrDefault(index));
			rule.Parent = this;
		}

		public override Annotation<TOffset> ApplyRhs(TData input, PatternMatch<TOffset> match, out TData output)
		{
			PatternRule<TData, TOffset> rule = _ruleIds[match.ExpressionPath.First()];
			var groups = new Dictionary<string, Span<TOffset>>();
			foreach (string group in match.Groups)
				groups[group] = match[group];
			return rule.ApplyRhs(input, new PatternMatch<TOffset>(match, groups, match.ExpressionPath.Skip(1), match.VariableBindings), out output);
		}
	}
}
