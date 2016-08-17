using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class EpenthesisAnalysisRewriteRuleSpec : RewriteRuleSpec
	{
		private readonly int _targetCount;

		public EpenthesisAnalysisRewriteRuleSpec(SpanFactory<ShapeNode> spanFactory, MatcherSettings<ShapeNode> matcherSettings, RewriteSubrule subrule)
			: base(false)
		{
			Pattern.Acceptable = IsUnapplicationNonvacuous;
			_targetCount = subrule.Rhs.Children.Count;

			foreach (Constraint<Word, ShapeNode> constraint in subrule.Rhs.Children.Cast<Constraint<Word, ShapeNode>>())
			{
				var newConstraint = constraint.DeepClone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
				Pattern.Children.Add(newConstraint);
			}
			Pattern.Freeze();

			SubruleSpecs.Add(new AnalysisRewriteSubruleSpec(spanFactory, matcherSettings, subrule, Unapply));
		}

		private static bool IsUnapplicationNonvacuous(Match<Word, ShapeNode> match)
		{
			foreach (ShapeNode node in match.Input.Shape.GetNodes(match.Span))
			{
				if (!node.Annotation.Optional)
					return true;
			}

			return false;
		}

		private void Unapply(Match<Word, ShapeNode> targetMatch, Span<ShapeNode> span, VariableBindings varBindings)
		{
			ShapeNode curNode = span.Start;
			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode.SetDirty(true);
				curNode = curNode.Next;
			}
		}
	}
}
