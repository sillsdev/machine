using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.PhonologicalRules
{
	public abstract class AnalysisRewriteRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern; 

		protected AnalysisRewriteRuleSpec()
		{
			_pattern = new Pattern<Word, ShapeNode>();
		}

		protected void AddEnvironment(string name, Pattern<Word, ShapeNode> env)
		{
			if (env.Children.Count == 0)
				return;

			_pattern.Children.Add(new Group<Word, ShapeNode>(name, CloneNodesExceptBoundaryConstraints(env.Children)));
		}

		private static IEnumerable<PatternNode<Word, ShapeNode>> CloneNodesExceptBoundaryConstraints(IEnumerable<PatternNode<Word, ShapeNode>> nodes)
		{
			foreach (PatternNode<Word, ShapeNode> node in nodes)
			{
				var constraint = node as Constraint<Word, ShapeNode>;
				if (constraint != null && constraint.Type() != HCFeatureSystem.Boundary)
				{
					yield return constraint.DeepClone();
					continue;
				}

				var alternation = node as Alternation<Word, ShapeNode>;
				if (alternation != null)
				{
					yield return new Alternation<Word, ShapeNode>(CloneNodesExceptBoundaryConstraints(alternation.Children));
					continue;
				}

				var group = node as Group<Word, ShapeNode>;
				if (group != null)
				{
					yield return new Group<Word, ShapeNode>(group.Name, CloneNodesExceptBoundaryConstraints(group.Children));
					continue;
				}

				var quantifier = node as Quantifier<Word, ShapeNode>;
				if (quantifier != null)
				{
					yield return new Quantifier<Word, ShapeNode>(quantifier.MinOccur, quantifier.MaxOccur, CloneNodesExceptBoundaryConstraints(quantifier.Children).SingleOrDefault());
					continue;
				}

				var pattern = node as Pattern<Word, ShapeNode>;
				if (pattern != null)
					yield return new Pattern<Word, ShapeNode>(pattern.Name, CloneNodesExceptBoundaryConstraints(pattern.Children));
			}
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public abstract ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output);
	}
}
