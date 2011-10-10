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
			var shape = (PhoneticShape) match.Start.List;
			PhoneticShapeNode curNode;
			Span<PhoneticShapeNode> leftEnv;
			if (match.TryGetGroup("leftEnv", out leftEnv))
			{
				curNode = leftEnv.End;
			}
			else
			{

				Span<PhoneticShapeNode> rightEnv;
				if (match.TryGetGroup("rightEnv", out rightEnv))
				{
					curNode = rightEnv.Start.Prev;
				}
				else
				{
					curNode = match.Start;
				}
			}

			foreach (PatternNode<PhoneticShapeNode> node in _rhs.Children.GetNodes(Lhs.Direction))
			{
				if (shape.Count == 256)
					throw new MorphException(MorphErrorCode.TooManySegs);
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
