using System;
using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public abstract class PatternRuleBatchBase<TData, TOffset> : PatternRuleBase<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly Dictionary<string, IPatternRule<TData, TOffset>> _rules;

		protected PatternRuleBatchBase(IEnumerable<IPatternRule<TData, TOffset>> rules)
			: base(new Pattern<TData, TOffset>(rules.First().Lhs.SpanFactory))
		{
			IPatternRule<TData, TOffset> firstRule = rules.First();
			_rules = new Dictionary<string, IPatternRule<TData, TOffset>>();
			foreach (IPatternRule<TData, TOffset> rule in rules)
			{
				if (rule.Lhs.Direction != firstRule.Lhs.Direction || rule.ApplicationMode != firstRule.ApplicationMode)
					throw new ArgumentException("The rules are not compatible.", "rules");
				AddRuleInternal(rule, true);
			}
			ApplicationMode = firstRule.ApplicationMode;
			Lhs.Direction = firstRule.Lhs.Direction;
		}

		protected PatternRuleBatchBase(Pattern<TData, TOffset> pattern)
			: base(pattern)
		{
			_rules = new Dictionary<string, IPatternRule<TData, TOffset>>();
		}

		public IEnumerable<IPatternRule<TData, TOffset>> Rules
		{
			get { return _rules.Values; }
		}

		protected void AddRuleInternal(IPatternRule<TData, TOffset> rule, bool end)
		{
			string id = "rule" + _rules.Count;
			_rules[id] = rule;
			var expr = new Expression<TData, TOffset>(id, rule.Lhs.Children.Clone())
			           	{
			           		Acceptable = (input, match) => rule.IsApplicable(input) && rule.Lhs.Acceptable(input, match)
			           	};

			Lhs.Children.Insert(expr, end ? Lhs.Children.Last : null);
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
