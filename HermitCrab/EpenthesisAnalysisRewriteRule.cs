using SIL.APRE;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class EpenthesisAnalysisRewriteRule : AnalysisRewriteRule
	{
		private readonly AnalysisReapplyType _reapplyType;
		private readonly int _targetCount;

		public EpenthesisAnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, Direction synthesisDir, ApplicationMode synthesisAppMode,
			Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv)
			: base(spanFactory)
		{
			ApplicationMode = ApplicationMode.Iterative;
			Lhs.Direction = synthesisDir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight;
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

			_reapplyType = synthesisAppMode == ApplicationMode.Simultaneous ? AnalysisReapplyType.SelfOpaquing : AnalysisReapplyType.Normal;
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
			ShapeNode curNode = target.GetStart(Lhs.Direction);
			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode = curNode.GetNext(Lhs.Direction);
			}

			ShapeNode resumeNode = match.GetStart(Lhs.Direction).GetNext(Lhs.Direction);
			MarkSearchedNodes(resumeNode, curNode);

			output = input;
			return resumeNode.Annotation;
		}
	}
}
