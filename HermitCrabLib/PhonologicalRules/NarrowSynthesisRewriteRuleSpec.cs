using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class NarrowSynthesisRewriteRuleSpec : SynthesisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs; 
		private readonly int _targetCount;

		public NarrowSynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, RewriteSubrule subrule)
			: base(lhs, subrule)
		{
			_rhs = subrule.Rhs;
			_targetCount = lhs.Children.Count;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			GroupCapture<ShapeNode> target = match.GroupCaptures["target"];
			ShapeNode endNode = target.Span.GetEnd(match.Matcher.Direction);
			foreach (PatternNode<Word, ShapeNode> node in _rhs.Children.GetNodes(match.Matcher.Direction))
			{
				var constraint = (Constraint<Word, ShapeNode>) node;
				FeatureStruct fs = constraint.FeatureStruct.DeepClone();
				fs.ReplaceVariables(match.VariableBindings);
				endNode = match.Input.Shape.AddAfter(endNode, fs);
				if (rule is BacktrackingPatternRule)
					endNode.SetDirty(true);
			}

			ShapeNode startNode = match.Span.GetStart(match.Matcher.Direction);
			ShapeNode resumeNode = startNode == target.Span.GetStart(match.Matcher.Direction)
				? target.Span.GetEnd(match.Matcher.Direction).GetNext(match.Matcher.Direction) : startNode;

			ShapeNode[] nodes = match.Input.Shape.GetNodes(target.Span, match.Matcher.Direction).ToArray();
			for (int i = 0; i < _targetCount; i++)
				nodes[i].SetDeleted(true);

			output = match.Input;
			return resumeNode;
		}
	}
}
