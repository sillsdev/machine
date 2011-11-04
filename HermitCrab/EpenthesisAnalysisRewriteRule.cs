using SIL.APRE;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class EpenthesisAnalysisRewriteRule : AnalysisRewriteRule
	{
		private AnalysisReapplyType _reapplyType;
		private readonly int _targetCount;

		public EpenthesisAnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> rhs,
			Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv)
			: base(spanFactory)
		{
			ApplicationMode = ApplicationMode.Iterative;
			Direction = Direction.RightToLeft;

			Lhs.Acceptable = (input, match) => IsUnapplicationNonvacuous(match);
			_targetCount = rhs.Children.Count;

			AddEnvironment("leftEnv", leftEnv);
			var target = new Group<Word, ShapeNode>("target");
			foreach (Constraint<Word, ShapeNode> constraint in rhs.Children)
			{
				var newConstraint = (Constraint<Word, ShapeNode>)constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
				target.Children.Add(newConstraint);
			}
			Lhs.Children.Add(target);
			AddEnvironment("rightEnv", rightEnv);
		}

		public override ApplicationMode SynthesisApplicationMode
		{
			set
			{
				base.SynthesisApplicationMode = value;
				_reapplyType = value == ApplicationMode.Simultaneous ? AnalysisReapplyType.SelfOpaquing : AnalysisReapplyType.Normal;
			}
		}

		public override Direction SynthesisDirection
		{
			set
			{
				base.SynthesisDirection = value;
				Direction = value == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight;
			}
		}

		private static bool IsUnapplicationNonvacuous(PatternMatch<ShapeNode> match)
		{
			Span<ShapeNode> target = match["target"];
			foreach (ShapeNode node in target.Start.GetNodes(target.End))
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

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			Span<ShapeNode> target = match["target"];
			ShapeNode curNode = target.GetStart(Direction);
			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode = curNode.GetNext(Direction);
			}

			ShapeNode resumeNode = match.GetStart(Direction).GetNext(Direction);
			MarkSearchedNodes(resumeNode, curNode);

			output = input;
			return resumeNode.Annotation;
		}
	}
}
