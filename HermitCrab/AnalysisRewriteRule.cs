using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class AnalysisRewriteRule : RuleCascade<Word, ShapeNode>
	{
		private readonly ApplicationMode _synthesisAppMode;
		private readonly int _delReapplications;

		public AnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, IEnumerable<AnalysisRewriteRuleSpec> ruleSpecs,
			ApplicationMode synthesisAppMode, Direction synthesisDir, int delReapplications)
			: base(CreateRules(spanFactory, ruleSpecs, synthesisDir))
		{
			_synthesisAppMode = synthesisAppMode;
			_delReapplications = delReapplications;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory,
			IEnumerable<AnalysisRewriteRuleSpec> ruleSpecs, Direction dir)
		{
			foreach (AnalysisRewriteRuleSpec ruleSpec in ruleSpecs)
			{
				var rule = new PatternRule<Word, ShapeNode>(spanFactory, ruleSpec, ruleSpec.ApplicationMode,
					new MatcherSettings<ShapeNode>
						{
							Direction = dir == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
							Filter = ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.AnchorType)
						});
				yield return rule;
			}
		}

		public override bool Apply(Word input, out IEnumerable<Word> output)
		{
			bool result = base.Apply(input, out output);

			return result;
		}

		protected override bool ApplyRule(IRule<Word, ShapeNode> rule, Word input, out IEnumerable<Word> output)
		{
			output = null;
			var rewriteRule = (PatternRule<Word, ShapeNode>) rule;
			switch (((AnalysisRewriteRuleSpec) rewriteRule.RuleSpec).GetAnalysisReapplyType(_synthesisAppMode))
			{
				case AnalysisReapplyType.Normal:
					{
						IEnumerable<Word> result;
						if (base.ApplyRule(rewriteRule, input, out result))
						{
							if (rewriteRule.ApplicationMode == ApplicationMode.Iterative)
								RemoveSearchedValue(input);
							output = result;
						}
					}
					break;

				case AnalysisReapplyType.Deletion:
					{
						int i = 0;
						IEnumerable<Word> result;
						while (i <= _delReapplications && base.ApplyRule(rewriteRule, input, out result))
						{
							input = result.Single();
							output = input.ToEnumerable();
							i++;
						}
					}
					break;

				case AnalysisReapplyType.SelfOpaquing:
					{
						IEnumerable<Word> result;
						while (base.ApplyRule(rewriteRule, input, out result))
						{
							if (rewriteRule.ApplicationMode == ApplicationMode.Iterative)
								RemoveSearchedValue(input);
							input = result.Single();
							output = input.ToEnumerable();
						}
					}
					break;
			}

			return output != null;
		}

		private void RemoveSearchedValue(IData<ShapeNode> input)
		{
			foreach (Annotation<ShapeNode> ann in input.Annotations)
				ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
		}
	}
}
