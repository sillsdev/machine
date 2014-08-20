using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class EpenthesisSynthesisRewriteRuleSpec : SynthesisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs;

		public EpenthesisSynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, RewriteSubrule subrule, int index)
			: base(lhs, subrule, index)
		{
			_rhs = subrule.Rhs;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			ShapeNode startNode;
			if (match.Matcher.Direction == Direction.LeftToRight)
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
			else
			{
				GroupCapture<ShapeNode> rightEnv = match.GroupCaptures["rightEnv"];
				if (rightEnv.Success)
				{
					startNode = rightEnv.Span.Start;
				}
				else
				{
					GroupCapture<ShapeNode> leftEnv = match.GroupCaptures["leftEnv"];
					startNode = leftEnv.Span.End.Next;
				}
			}

			ShapeNode curNode = startNode;
			foreach (PatternNode<Word, ShapeNode> node in _rhs.Children.GetNodes(match.Matcher.Direction))
			{
				if (match.Input.Shape.Count == 256)
					throw new InfiniteLoopException("An epenthesis rewrite rule is stuck in an infinite loop.");
				var constraint = (Constraint<Word, ShapeNode>) node;
				FeatureStruct fs = constraint.FeatureStruct.DeepClone();
				fs.ReplaceVariables(match.VariableBindings);
				curNode = match.Input.Shape.AddAfter(curNode, fs);
				if (rule is BacktrackingPatternRule)
					curNode.SetDirty(true);
			}
			output = match.Input;
			MarkSuccessfulApply(output);
			return startNode.GetNext(match.Matcher.Settings.Direction);
		}
	}
}
