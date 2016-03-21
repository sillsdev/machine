using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.PhonologicalRules
{
	public class NarrowAnalysisRewriteRuleSpec : AnalysisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _analysisRhs;
		private readonly int _targetCount;

		public NarrowAnalysisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, RewriteSubrule subrule)
		{
			_analysisRhs = lhs;
			_targetCount = subrule.Rhs.Children.Count;

			AddEnvironment("leftEnv", subrule.LeftEnvironment);
			if (subrule.Rhs.Children.Count > 0)
				Pattern.Children.Add(new Group<Word, ShapeNode>("target", subrule.Rhs.Children.DeepClone()));
			AddEnvironment("rightEnv", subrule.RightEnvironment);
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			ShapeNode startNode;
			GroupCapture<ShapeNode> target = match.GroupCaptures["target"];
			if (target.Success)
			{
				startNode = target.Span.GetEnd(match.Matcher.Direction);
			}
			else
			{
				GroupCapture<ShapeNode> leftEnv = match.GroupCaptures["leftEnv"];
				if (leftEnv.Success)
				{
					startNode = leftEnv.Span.End;
				}
				else
				{
					GroupCapture<ShapeNode> rightEnv = match.GroupCaptures["rightEnv"];
					startNode = rightEnv.Span.Start.Prev;
				}
			}

			ShapeNode curNode = startNode;
			foreach (Constraint<Word, ShapeNode> constraint in _analysisRhs.Children)
			{
				FeatureStruct fs = constraint.FeatureStruct.DeepClone();
				fs.ReplaceVariables(match.VariableBindings);
				curNode = match.Input.Shape.AddAfter(curNode, fs, true);
			}

			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode = curNode.Next;
			}

			output = match.Input;
			return null;
		}

	}
}
