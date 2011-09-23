using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public class EpenthesisSynthesisRewriteRule : SynthesisRewriteRule
	{
		private readonly Expression<PhoneticShapeNode> _rhs; 

		public EpenthesisSynthesisRewriteRule(SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv, FeatureStruct applicableFS)
			: base(spanFactory, dir, simult, lhs, leftEnv, rightEnv, applicableFS)
		{
			_rhs = rhs;
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			PhoneticShape shape;
			PhoneticShapeNode curNode;
			Span<PhoneticShapeNode> leftEnv;
			if (match.TryGetGroup("leftEnv", out leftEnv))
			{
				shape = (PhoneticShape)leftEnv.Start.List;
				curNode = leftEnv.End;
			}
			else
			{
				Span<PhoneticShapeNode> rightEnv = match["rightEnv"];
				shape = (PhoneticShape)rightEnv.Start.List;
				curNode = rightEnv.Start.Prev;
			}

			foreach (PatternNode<PhoneticShapeNode> node in _rhs.Children.GetNodes(Lhs.Direction))
			{
				var constraint = (Constraint<PhoneticShapeNode>) node;
				PhoneticShapeNode newNode = CreateNodeFromConstraint(constraint, match.VariableBindings);
				shape.Insert(newNode, curNode, Lhs.Direction);
				curNode = newNode;
			}

			MarkSearchedNodes(match.GetStart(Lhs.Direction), curNode);

			return match.GetStart(Lhs.Direction).Annotation;
		}
	}
}
