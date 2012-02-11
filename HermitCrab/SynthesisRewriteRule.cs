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
			: base(spanFactory, new BatchPatternRuleSpec<Word, ShapeNode>(ruleSpecs), appMode, new MatcherSettings<ShapeNode>
			         	{
			         		Direction = dir,
							Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.BoundaryType, HCFeatureSystem.AnchorType),
							UseDefaultsForMatching = true
			         	})
		{
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			Word output = base.Apply(input).SingleOrDefault();
			if (output != null)
			{
				if (ApplicationMode == ApplicationMode.Iterative)
				{
					foreach (Annotation<ShapeNode> ann in output.Annotations)
						ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
				}
				return output.ToEnumerable();
			}

			return Enumerable.Empty<Word>();
		}
	}
}
