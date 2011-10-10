using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public class NarrowSynthesisRewriteRule : SynthesisRewriteRule
	{
		private readonly Expression<PhoneticShapeNode> _rhs; 
		private readonly int _targetCount;

		public NarrowSynthesisRewriteRule(SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv,
			FeatureStruct applicableFS)
			: base(spanFactory, dir, simult, lhs, leftEnv, rightEnv, applicableFS)
		{
			_rhs = rhs;
			_targetCount = lhs.Children.Count;
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			Span<PhoneticShapeNode> target = match["target"];
			PhoneticShapeNode curNode = target.GetEnd(Lhs.Direction);
			var shape = (PhoneticShape) curNode.List;
			foreach (PatternNode<PhoneticShapeNode> node in _rhs.Children.GetNodes(Lhs.Direction))
			{
				var constraint = (Constraint<PhoneticShapeNode>) node;
				PhoneticShapeNode newNode = CreateNodeFromConstraint(constraint, match.VariableBindings);
				shape.Insert(newNode, curNode, Lhs.Direction);
				curNode = newNode;
			}

			MarkSearchedNodes(match.GetStart(Lhs.Direction), curNode);
			PhoneticShapeNode resumeNode = match.GetStart(Lhs.Direction).GetPrev(Lhs.Direction);

			PhoneticShapeNode[] nodes = target.GetStart(Lhs.Direction).GetNodes(target.GetEnd(Lhs.Direction)).ToArray();
			for (int i = 0; i < _targetCount; i++)
				nodes[i].Remove();

			return resumeNode == null ? null : resumeNode.Annotation;
		}
	}
}
