using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class SynthesisRewriteRuleBatch : PatternRuleBatch<Word, ShapeNode>
	{
		public SynthesisRewriteRuleBatch(SpanFactory<ShapeNode> spanFactory)
			: base(new Pattern<Word, ShapeNode>(spanFactory) {UseDefaultsForMatching = true,
				Filter = ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.BoundaryType, HCFeatureSystem.AnchorType)})
		{
			ApplicationMode = ApplicationMode.Iterative;
		}

		public void AddSubrule(SynthesisRewriteRule rule)
		{
			InsertRuleInternal(Rules.Count, rule);
		}

		public override bool IsApplicable(Word input)
		{
			return true;
		}

		public override bool Apply(Word input, out IEnumerable<Word> output)
		{
			if (base.Apply(input, out output))
			{
				foreach (Annotation<ShapeNode> ann in input.Annotations)
					ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
				return true;
			}
			return false;
		}
	}
}
