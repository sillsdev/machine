using System.Linq;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
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
				env.Children.Where(node => !(node is Constraint<Word, ShapeNode>) || ((Constraint<Word, ShapeNode>)node).Type != HCFeatureSystem.BoundaryType).Clone()));
		}

		protected void MarkSearchedNodes(ShapeNode startNode, ShapeNode endNode, Direction dir)
		{
			foreach (ShapeNode node in startNode.GetNodes(endNode, dir))
				node.Annotation.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.Searched);
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
