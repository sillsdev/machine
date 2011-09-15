using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public abstract class PatternRuleBatchBase<TOffset> : PatternRuleBase<TOffset>
	{
		private readonly Dictionary<string, IPatternRule<TOffset>> _rules;

		protected PatternRuleBatchBase(IEnumerable<IPatternRule<TOffset>> rules)
			: base(CreatePattern(rules), rules.First().Simultaneous)
		{
			_rules = new Dictionary<string, IPatternRule<TOffset>>();
			foreach (IPatternRule<TOffset> rule in rules)
				AddRuleInternal(rule);
		}

		protected PatternRuleBatchBase(Pattern<TOffset> pattern, bool simult)
			: base(pattern, simult)
		{
			_rules = new Dictionary<string, IPatternRule<TOffset>>();
		}

		private static Pattern<TOffset> CreatePattern(IEnumerable<IPatternRule<TOffset>> rules)
		{
			IPatternRule<TOffset> firstRule = rules.First();
			return new Pattern<TOffset>(firstRule.Lhs.SpanFactory, firstRule.Lhs.Direction, firstRule.Lhs.Filter);
		}

		public IEnumerable<IPatternRule<TOffset>> Rules
		{
			get { return _rules.Values; }
		}

		protected void AddRuleInternal(IPatternRule<TOffset> rule)
		{
			string id = "rule" + _rules.Count;
			_rules[id] = rule;
			Lhs.Children.Add(new Expression<TOffset>(id, (input, match) => rule.IsApplicable(input) && rule.Lhs.Acceptable(input, match),
				rule.Lhs.Children.Clone()));
		}

		public override Annotation<TOffset> ApplyRhs(IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match)
		{
			IPatternRule<TOffset> rule = _rules[match.ExpressionPath.First()];
			var groups = new Dictionary<string, Span<TOffset>>();
			foreach (string group in match.Groups)
				groups[group] = match[group];
			return rule.ApplyRhs(input, new PatternMatch<TOffset>(match, groups, match.ExpressionPath.Skip(1), match.VariableBindings));
		}
	}
}
