using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public enum AnalysisReapplyType
	{
		Normal,
		Deletion,
		SelfOpaquing
	}

	public abstract class AnalysisRewriteRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern; 

		protected AnalysisRewriteRuleSpec()
		{
			_pattern = new Pattern<Word, ShapeNode>();
		}

		public abstract AnalysisReapplyType GetAnalysisReapplyType(ApplicationMode synthesisAppMode);

		public abstract ApplicationMode ApplicationMode { get; }

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
