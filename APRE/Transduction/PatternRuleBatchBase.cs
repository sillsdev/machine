using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public abstract class PatternRuleBatchBase<TData, TOffset> : PatternRuleBase<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly Dictionary<string, IPatternRule<TData, TOffset>> _rules;

		protected PatternRuleBatchBase(IEnumerable<IPatternRule<TData, TOffset>> rules)
			: base(CreatePattern(rules), rules.First().ApplicationMode)
		{
			_rules = new Dictionary<string, IPatternRule<TData, TOffset>>();
			foreach (IPatternRule<TData, TOffset> rule in rules)
				AddRuleInternal(rule, true);
		}

		protected PatternRuleBatchBase(Pattern<TData, TOffset> pattern, ApplicationMode appMode)
			: base(pattern, appMode)
		{
			_rules = new Dictionary<string, IPatternRule<TData, TOffset>>();
		}

		private static Pattern<TData, TOffset> CreatePattern(IEnumerable<IPatternRule<TData, TOffset>> rules)
		{
			IPatternRule<TData, TOffset> firstRule = rules.First();
			return new Pattern<TData, TOffset>(firstRule.Lhs.SpanFactory, firstRule.Lhs.Direction, firstRule.Lhs.Filter);
		}

		public IEnumerable<IPatternRule<TData, TOffset>> Rules
		{
			get { return _rules.Values; }
		}

		protected void AddRuleInternal(IPatternRule<TData, TOffset> rule, bool end)
		{
			string id = "rule" + _rules.Count;
			_rules[id] = rule;
			Lhs.Children.Insert(new Expression<TData, TOffset>(id, (input, match) => rule.IsApplicable(input) && rule.Lhs.Acceptable(input, match),
				rule.Lhs.Children.Clone()), end ? Lhs.Children.Last : null);
		}

		public override Annotation<TOffset> ApplyRhs(TData input, PatternMatch<TOffset> match, out TData output)
		{
			IPatternRule<TData, TOffset> rule = _rules[match.ExpressionPath.First()];
			var groups = new Dictionary<string, Span<TOffset>>();
			foreach (string group in match.Groups)
				groups[group] = match[group];
			return rule.ApplyRhs(input, new PatternMatch<TOffset>(match, groups, match.ExpressionPath.Skip(1), match.VariableBindings), out output);
		}
	}
}
