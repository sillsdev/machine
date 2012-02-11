using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class NarrowAnalysisRewriteRuleSpec : AnalysisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _analysisRhs;
		private readonly int _targetCount;

		public NarrowAnalysisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, Pattern<Word, ShapeNode> rhs,
			Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv)
		{
			_analysisRhs = lhs;
			_targetCount = rhs.Children.Count;

			AddEnvironment("leftEnv", leftEnv);
			if (rhs.Children.Count > 0)
				Pattern.Children.Add(new Group<Word, ShapeNode>("target", rhs.Children.Clone()));
			AddEnvironment("rightEnv", rightEnv);
		}

		public override ApplicationMode ApplicationMode
		{
			get { return ApplicationMode.Simultaneous; }
		}

		public override AnalysisReapplyType GetAnalysisReapplyType(ApplicationMode synthesisAppMode)
		{
			return AnalysisReapplyType.Deletion;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			ShapeNode startNode;
			GroupCapture<ShapeNode> target = match["target"];
			if (target.Success)
			{
				startNode = target.Span.GetEnd(match.Matcher.Direction);
			}
			else
			{
				GroupCapture<ShapeNode> leftEnv = match["leftEnv"];
				if (leftEnv.Success)
				{
					startNode = leftEnv.Span.End;
				}
				else
				{
					GroupCapture<ShapeNode> rightEnv = match["rightEnv"];
					startNode = rightEnv.Span.Start.Prev;
				}
			}

			ShapeNode curNode = startNode;
			foreach (Constraint<Word, ShapeNode> constraint in _analysisRhs.Children)
			{
				FeatureStruct fs = constraint.FeatureStruct.Clone();
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
