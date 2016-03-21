using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.PhonologicalRules
{
	public class EpenthesisAnalysisRewriteRuleSpec : AnalysisRewriteRuleSpec
	{
		private readonly int _targetCount;

		public EpenthesisAnalysisRewriteRuleSpec(RewriteSubrule subrule)
		{
			Pattern.Acceptable = IsUnapplicationNonvacuous;
			_targetCount = subrule.Rhs.Children.Count;

			AddEnvironment("leftEnv", subrule.LeftEnvironment);
			var target = new Group<Word, ShapeNode>("target");
			foreach (Constraint<Word, ShapeNode> constraint in subrule.Rhs.Children.Cast<Constraint<Word, ShapeNode>>())
			{
				var newConstraint = constraint.DeepClone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
				target.Children.Add(newConstraint);
			}
			Pattern.Children.Add(target);
			AddEnvironment("rightEnv", subrule.RightEnvironment);
		}

		private static bool IsUnapplicationNonvacuous(Match<Word, ShapeNode> match)
		{
			GroupCapture<ShapeNode> target = match.GroupCaptures["target"];
			foreach (ShapeNode node in match.Input.Shape.GetNodes(target.Span))
			{
				if (!node.Annotation.Optional)
					return true;
			}

			return false;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			GroupCapture<ShapeNode> target = match.GroupCaptures["target"];
			ShapeNode curNode = target.Span.GetStart(match.Matcher.Direction);
			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode.SetDirty(true);
				curNode = curNode.GetNext(match.Matcher.Direction);
			}

			output = match.Input;
			return match.Span.GetStart(match.Matcher.Direction).GetNext(match.Matcher.Direction);
		}
	}
}
