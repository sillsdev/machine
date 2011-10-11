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

	public abstract class AnalysisRewriteRule : PatternRuleBase<PhoneticShapeNode>
	{
		protected AnalysisRewriteRule(SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult,
			Func<IBidirList<Annotation<PhoneticShapeNode>>, PatternMatch<PhoneticShapeNode>, bool> acceptable)
			: base(new Pattern<PhoneticShapeNode>(spanFactory, dir, ann => ann.Type != HCFeatureSystem.BoundaryType, acceptable), simult)
		{
		}

		public abstract AnalysisReapplyType AnalysisReapplyType { get; }

		protected void AddEnvironment(string name, Expression<PhoneticShapeNode> env)
		{
			if (env.Children.Count == 0)
				return;

			Lhs.Children.Add(new Group<PhoneticShapeNode>(name,
				env.Children.Where(node => !(node is Constraint<PhoneticShapeNode>) || ((Constraint<PhoneticShapeNode>) node).Type != HCFeatureSystem.BoundaryType).Clone()));
		}

		protected void MarkSearchedNodes(PhoneticShapeNode startNode, PhoneticShapeNode endNode)
		{
			foreach (PhoneticShapeNode node in endNode == null ? startNode.GetNodes(Lhs.Direction) : startNode.GetNodes(endNode, Lhs.Direction))
				node.Annotation.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.Searched);
		}

		public override bool IsApplicable(IBidirList<Annotation<PhoneticShapeNode>> input)
		{
			return true;
		}
	}
}
