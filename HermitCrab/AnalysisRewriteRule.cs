using System;
using System.Linq;
using SIL.APRE;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public enum AnalysisReapplyType
	{
		Normal,
		Deletion,
		SelfOpaquing
	}

	public abstract class AnalysisRewriteRule : PatternRuleBase<Word, ShapeNode>
	{
		protected AnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, Direction dir, ApplicationMode appMode,
			Func<Word, PatternMatch<ShapeNode>, bool> acceptable)
			: base(new Pattern<Word, ShapeNode>(spanFactory, dir, ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.AnchorType), acceptable), appMode)
		{
		}

		public abstract AnalysisReapplyType AnalysisReapplyType { get; }

		protected void AddEnvironment(string name, Expression<Word, ShapeNode> env)
		{
			if (env.Children.Count == 0)
				return;

			Lhs.Children.Add(new Group<Word, ShapeNode>(name,
				env.Children.Where(node => !(node is Constraint<Word, ShapeNode>) || ((Constraint<Word, ShapeNode>)node).Type != HCFeatureSystem.BoundaryType).Clone()));
		}

		protected void MarkSearchedNodes(ShapeNode startNode, ShapeNode endNode)
		{
			if (ApplicationMode == ApplicationMode.Iterative)
			{
				foreach (ShapeNode node in startNode.GetNodes(endNode, Lhs.Direction))
					node.Annotation.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.Searched);
			}
		}

		public override bool IsApplicable(Word input)
		{
			return true;
		}
	}
}
