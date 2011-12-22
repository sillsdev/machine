using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class EpenthesisSynthesisRewriteRuleSpec : SynthesisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs;

		public EpenthesisSynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs,
			Pattern<Word, ShapeNode> rhs, Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv, FeatureStruct requiredSyntacticFS)
			: base(lhs, leftEnv, rightEnv, requiredSyntacticFS)
		{
			_rhs = rhs;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			ShapeNode startNode;
			if (match.Matcher.Direction == Direction.LeftToRight)
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
			else
			{
				GroupCapture<ShapeNode> rightEnv = match["rightEnv"];
				if (rightEnv.Success)
				{
					startNode = rightEnv.Span.Start;
				}
				else
				{
					GroupCapture<ShapeNode> leftEnv = match["leftEnv"];
					startNode = leftEnv.Span.End.Next;
				}
			}

			ShapeNode curNode = startNode;
			foreach (PatternNode<Word, ShapeNode> node in _rhs.Children.GetNodes(match.Matcher.Direction))
			{
				if (match.Input.Shape.Count == 256)
					throw new MorphException(MorphErrorCode.TooManySegs);
				var constraint = (Constraint<Word, ShapeNode>) node;
				if (match.VariableBindings.Values.OfType<SymbolicFeatureValue>().Where(value => value.Feature.DefaultValue.Equals(value)).Any())
					throw new MorphException(MorphErrorCode.UninstantiatedFeature);
				FeatureStruct fs = constraint.FeatureStruct.Clone();
				fs.ReplaceVariables(match.VariableBindings);
				curNode = match.Input.Shape.AddAfter(curNode, constraint.Type, fs);
			}
			if (rule.ApplicationMode == ApplicationMode.Iterative)
				MarkSearchedNodes(startNode, curNode, match.Matcher.Direction);
			output = match.Input;
			return startNode.GetNext(match.Matcher.Settings.Direction);
		}
	}
}
