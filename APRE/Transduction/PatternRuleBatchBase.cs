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
			int i = 0;
			foreach (IPatternRule<TOffset> rule in rules)
				_rules["rule" + i++] = rule;
		}

		private static Pattern<TOffset> CreatePattern(IEnumerable<IPatternRule<TOffset>> rules)
		{
			IPatternRule<TOffset> firstRule = rules.First();
			var pattern = new Pattern<TOffset>(firstRule.Lhs.SpanFactory, firstRule.Lhs.Direction, firstRule.Lhs.Filter);
			int i = 0;
			foreach (IPatternRule<TOffset> rule in rules)
			{
				IPatternRule<TOffset> localRule = rule;
				pattern.Children.Add(new Expression<TOffset>("rule" + i,
					(input, match) => localRule.IsApplicable(input) && (localRule.Lhs.Acceptable == null || localRule.Lhs.Acceptable(input, match)),
					rule.Lhs.Children.Clone()));
				i++;
			}
			return pattern;
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
