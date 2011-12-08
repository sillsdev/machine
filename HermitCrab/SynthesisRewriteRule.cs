using System.Linq;
using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class SynthesisRewriteRule : PatternRule<Word, ShapeNode>
	{
		public SynthesisRewriteRule(SpanFactory<ShapeNode> spanFactory, IEnumerable<SynthesisRewriteRuleSpec> ruleSpecs,
			ApplicationMode appMode, Direction dir)
			: base(spanFactory, new BatchPatternRuleSpec<Word, ShapeNode>(ruleSpecs.Cast<IPatternRuleSpec<Word, ShapeNode>>()),
			appMode, new MatcherSettings<ShapeNode>
			         	{
			         		Direction = dir,
							Filter = ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.BoundaryType, HCFeatureSystem.AnchorType),
							UseDefaultsForMatching = true
			         	})
		{
		}

		public override bool Apply(Word input, out IEnumerable<Word> output)
		{
			if (base.Apply(input, out output))
			{
				if (ApplicationMode == ApplicationMode.Iterative)
				{
					foreach (Annotation<ShapeNode> ann in input.Annotations)
						ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
				}
				return true;
			}
			return false;
		}
	}
}
