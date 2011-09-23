using SIL.APRE;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public class EpenthesisAnalysisRewriteRule : AnalysisRewriteRule
	{
		private readonly AnalysisReapplyType _reapplyType;
		private readonly int _targetCount;

		public EpenthesisAnalysisRewriteRule(SpanFactory<PhoneticShapeNode> spanFactory, Direction synthesisDir, bool synthesisSimult,
			Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv)
			: base(spanFactory, synthesisDir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight, false,
			(input, match) => IsUnapplicationNonvacuous(match))
		{
			_targetCount = rhs.Children.Count;

			AddEnvironment("leftEnv", leftEnv);
			var target = new Group<PhoneticShapeNode>("target");
			foreach (Constraint<PhoneticShapeNode> constraint in rhs.Children)
			{
				var newConstraint = (Constraint<PhoneticShapeNode>) constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
				target.Children.Add(newConstraint);
			}
			Lhs.Children.Add(target);
			AddEnvironment("rightEnv", rightEnv);

			_reapplyType = synthesisSimult ? AnalysisReapplyType.SelfOpaquing : AnalysisReapplyType.Normal;
		}

		private static bool IsUnapplicationNonvacuous(PatternMatch<PhoneticShapeNode> match)
		{
			Span<PhoneticShapeNode> target = match["target"];
			foreach (PhoneticShapeNode node in target.Start.GetNodes(target.End))
			{
				if (!node.Annotation.Optional)
					return true;
			}

			return false;
		}

		public override AnalysisReapplyType AnalysisReapplyType
		{
			get { return _reapplyType; }
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			Span<PhoneticShapeNode> target = match["target"];
			PhoneticShapeNode curNode = target.GetStart(Lhs.Direction);
			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode = curNode.GetNext(Lhs.Direction);
			}

			MarkSearchedNodes(match.GetStart(Lhs.Direction), curNode);

			return match.GetStart(Lhs.Direction).Annotation;
		}
	}
}
