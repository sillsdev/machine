using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
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

			_pattern.Children.Add(new Group<Word, ShapeNode>(name,
				env.Children.Where(node => !(node is Constraint<Word, ShapeNode>) || ((Constraint<Word, ShapeNode>)node).Type() != HCFeatureSystem.Boundary).DeepClone()));
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
