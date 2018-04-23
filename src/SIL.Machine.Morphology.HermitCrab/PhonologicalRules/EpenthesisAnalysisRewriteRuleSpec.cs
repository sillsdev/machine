using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public class EpenthesisAnalysisRewriteRuleSpec : RewriteRuleSpec
	{
		private readonly int _targetCount;

		public EpenthesisAnalysisRewriteRuleSpec(MatcherSettings<ShapeNode> matcherSettings, RewriteSubrule subrule)
			: base(false)
		{
			Pattern.Acceptable = IsUnapplicationNonvacuous;
			_targetCount = subrule.Rhs.Children.Count;

			foreach (Constraint<Word, ShapeNode> constraint in subrule.Rhs.Children.Cast<Constraint<Word, ShapeNode>>())
			{
				Constraint<Word, ShapeNode> newConstraint = constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
				Pattern.Children.Add(newConstraint);
			}
			Pattern.Freeze();

			SubruleSpecs.Add(new AnalysisRewriteSubruleSpec(matcherSettings, subrule, Unapply));
		}

		private static bool IsUnapplicationNonvacuous(Match<Word, ShapeNode> match)
		{
			foreach (ShapeNode node in match.Input.Shape.GetNodes(match.Range))
			{
				if (!node.Annotation.Optional)
					return true;
			}

			return false;
		}

		private void Unapply(Match<Word, ShapeNode> targetMatch, Range<ShapeNode> range, VariableBindings varBindings)
		{
			ShapeNode curNode = range.Start;
			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode.SetDirty(true);
				curNode = curNode.Next;
			}
		}
	}
}
