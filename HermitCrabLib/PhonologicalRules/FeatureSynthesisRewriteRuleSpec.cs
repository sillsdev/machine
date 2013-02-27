using System;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class FeatureSynthesisRewriteRuleSpec : SynthesisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs;

		public FeatureSynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, RewriteSubrule subrule)
			: base(lhs, subrule)
		{
			_rhs = subrule.Rhs;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			GroupCapture<ShapeNode> target = match.GroupCaptures["target"];
			foreach (Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in match.Input.Shape.GetNodes(target.Span).Zip(_rhs.Children))
			{
				var constraints = (Constraint<Word, ShapeNode>) tuple.Item2;
				tuple.Item1.Annotation.FeatureStruct.PriorityUnion(constraints.FeatureStruct, match.VariableBindings);
				if (rule is BacktrackingPatternRule)
					tuple.Item1.SetDirty(true);
			}

			output = match.Input;
			return match.Span.GetStart(match.Matcher.Direction).GetNext(match.Matcher.Direction);
		}
	}
}
