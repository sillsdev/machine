using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Patterns;

namespace SIL.APRE.Rules
{
	public class RuleBatch<TOffset> : Rule<TOffset>
	{
		public RuleBatch(IEnumerable<Rule<TOffset>> rules)
			: base(CreatePattern(rules), new RuleAction(rules), rules.First().Simultaneous)
		{
		}

		private static Pattern<TOffset> CreatePattern(IEnumerable<Rule<TOffset>> rules)
		{
			Rule<TOffset> firstRule = rules.First();
			var pattern = new Pattern<TOffset>(firstRule.Lhs.SpanFactory, firstRule.Lhs.Direction, firstRule.Lhs.Filter);
			var groups = new List<PatternNode<TOffset>>();
			int i = 0;
			foreach (Rule<TOffset> rule in rules)
			{
				string prefix = "rule" + i;
				groups.Add(new Group<TOffset>("rule" + i, RenameGroups(prefix, rule.Lhs.Root.Children)));
				i++;
			}
			pattern.Root.Children.Add(new Alternation<TOffset>(groups));
			return pattern;
		}

		private static IEnumerable<PatternNode<TOffset>> RenameGroups(string prefix, IEnumerable<PatternNode<TOffset>> nodes)
		{

			foreach (PatternNode<TOffset> node in nodes)
			{
				PatternNode<TOffset> newChild = null;
				switch (node.Type)
				{
					case PatternNodeType.Alternation:
						newChild = new Alternation<TOffset>();
						break;

					case PatternNodeType.Constraints:
						newChild = node.Clone();
						break;

					case PatternNodeType.Group:
						var group = (Group<TOffset>) node;
						newChild = new Group<TOffset>(group.GroupName != null ? prefix + group.GroupName : null);
						break;

					case PatternNodeType.Quantifier:
						var quantifier = (Quantifier<TOffset>) node;
						newChild = new Quantifier<TOffset>(quantifier.MinOccur, quantifier.MaxOccur);
						break;
				}

				if (newChild != null)
				{
					foreach (PatternNode<TOffset> renamedChild in RenameGroups(prefix, node.Children))
						newChild.Children.Add(renamedChild);
				}
				yield return newChild;
			}
		}

		private class RuleAction : IRuleAction<TOffset>
		{
			private readonly List<Rule<TOffset>> _rules;

			public RuleAction(IEnumerable<Rule<TOffset>> rules)
			{
				_rules = new List<Rule<TOffset>>(rules);
			}

			public Annotation<TOffset> Run(Pattern<TOffset> lhs, IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match)
			{
				for (int i = 0; i < _rules.Count; i++)
				{
					string prefix = "rule" + i;
					Span<TOffset> span;
					if (match.TryGetGroup(prefix, out span))
					{
						var groups = new Dictionary<string, Span<TOffset>>();
						foreach (string group in match.Groups)
						{
							if (group != prefix && group.StartsWith(prefix))
								groups[group.Remove(0, prefix.Length)] = match[group];
						}
						return _rules[i].Rhs.Run(_rules[i].Lhs, input, new PatternMatch<TOffset>(span, groups, match.VariableBindings));
					}
				}

				return null;
			}
		}
	}
}
