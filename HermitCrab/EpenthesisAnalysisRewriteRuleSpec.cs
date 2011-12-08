using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class EpenthesisAnalysisRewriteRuleSpec : AnalysisRewriteRuleSpec
	{
		private readonly int _targetCount;

		public EpenthesisAnalysisRewriteRuleSpec(Pattern<Word, ShapeNode> rhs,
			Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv)
		{
			Pattern.Acceptable = IsUnapplicationNonvacuous;
			_targetCount = rhs.Children.Count;

			AddEnvironment("leftEnv", leftEnv);
			var target = new Group<Word, ShapeNode>("target");
			foreach (Constraint<Word, ShapeNode> constraint in rhs.Children)
			{
				var newConstraint = (Constraint<Word, ShapeNode>)constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
				target.Children.Add(newConstraint);
			}
			Pattern.Children.Add(target);
			AddEnvironment("rightEnv", rightEnv);
		}

		public override ApplicationMode ApplicationMode
		{
			get { return ApplicationMode.Iterative; }
		}

		private static bool IsUnapplicationNonvacuous(Match<Word, ShapeNode> match)
		{
			GroupCapture<ShapeNode> target = match["target"];
			foreach (ShapeNode node in match.Input.Shape.GetNodes(target.Span))
			{
				if (!node.Annotation.Optional)
					return true;
			}

			return false;
		}

		public override AnalysisReapplyType GetAnalysisReapplyType(ApplicationMode synthesisAppMode)
		{
			return synthesisAppMode == ApplicationMode.Simultaneous ? AnalysisReapplyType.SelfOpaquing : AnalysisReapplyType.Normal;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			GroupCapture<ShapeNode> target = match["target"];
			ShapeNode curNode = target.Span.GetStart(match.Matcher.Direction);
			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode = curNode.GetNext(match.Matcher.Direction);
			}

			ShapeNode resumeNode = match.Span.GetStart(match.Matcher.Direction).GetNext(match.Matcher.Direction);
			MarkSearchedNodes(resumeNode, curNode, match.Matcher.Direction);

			output = match.Input;
			return resumeNode;
		}
	}
}
