using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class NarrowSynthesisRewriteRuleSpec : SynthesisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs; 
		private readonly int _targetCount;

		public NarrowSynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs,
			Pattern<Word, ShapeNode> rhs, Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv,
			FeatureStruct requiredSyntacticFS)
			: base(lhs, leftEnv, rightEnv, requiredSyntacticFS)
		{
			_rhs = rhs;
			_targetCount = lhs.Children.Count;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			GroupCapture<ShapeNode> target = match["target"];
			ShapeNode curNode = target.Span.GetEnd(match.Matcher.Direction);
			foreach (PatternNode<Word, ShapeNode> node in _rhs.Children.GetNodes(match.Matcher.Direction))
			{
				var constraint = (Constraint<Word, ShapeNode>) node;
				if (match.VariableBindings.Values.OfType<SymbolicFeatureValue>().Any(value => value.Feature.DefaultValue.Equals(value)))
					throw new MorphException(MorphErrorCode.UninstantiatedFeature);
				FeatureStruct fs = constraint.FeatureStruct.Clone();
				fs.ReplaceVariables(match.VariableBindings);
				curNode = match.Input.Shape.AddAfter(curNode, fs);
			}

			ShapeNode matchStartNode = match.Span.GetStart(match.Matcher.Direction);
			ShapeNode resumeNode = matchStartNode == target.Span.GetStart(match.Matcher.Direction)
				? target.Span.GetEnd(match.Matcher.Direction).GetNext(match.Matcher.Direction) : matchStartNode;
			if (rule.ApplicationMode == ApplicationMode.Iterative)
				MarkSearchedNodes(resumeNode, curNode, match.Matcher.Direction);

			ShapeNode[] nodes = match.Input.Shape.GetNodes(target.Span, match.Matcher.Direction).ToArray();
			for (int i = 0; i < _targetCount; i++)
				nodes[i].Remove();

			output = match.Input;
			return resumeNode;
		}
	}
}
