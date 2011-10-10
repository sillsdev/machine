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
			: base(new Pattern<PhoneticShapeNode>(spanFactory, dir, ann => ann.Type == "segment", acceptable), simult)
		{
		}

		public abstract AnalysisReapplyType AnalysisReapplyType { get; }

		protected void AddEnvironment(string name, Expression<PhoneticShapeNode> env)
		{
			if (env.Children.Count == 0)
				return;

			if (env.Children.First is Anchor<PhoneticShapeNode>)
				Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorType.LeftSide));
			Lhs.Children.Add(new Group<PhoneticShapeNode>(name, from node in env.Children
															    where !(node is Anchor<PhoneticShapeNode>) && (!(node is Constraint<PhoneticShapeNode>)
																	|| ((Constraint<PhoneticShapeNode>)node).Type == "segment")
																select node.Clone()));
			if (env.Children.Last is Anchor<PhoneticShapeNode>)
				Lhs.Children.Add(new Anchor<PhoneticShapeNode>(AnchorType.RightSide));
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
