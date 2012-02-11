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
							Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.AnchorType)
						});
				yield return rule;
			}
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			IEnumerable<Word> output = base.Apply(input);

			return output;
		}

		protected override IEnumerable<Word> ApplyRule(IRule<Word, ShapeNode> rule, Word input)
		{
			var rewriteRule = (PatternRule<Word, ShapeNode>) rule;
			switch (((AnalysisRewriteRuleSpec) rewriteRule.RuleSpec).GetAnalysisReapplyType(_synthesisAppMode))
			{
				case AnalysisReapplyType.Normal:
					{
						Word output = base.ApplyRule(rewriteRule, input).SingleOrDefault();
						if (output != null)
						{
							if (rewriteRule.ApplicationMode == ApplicationMode.Iterative)
								RemoveSearchedValue(output);
							return output.ToEnumerable();
						}
					}
					break;

				case AnalysisReapplyType.Deletion:
					{
						Word output = null;
						int i = 0;
						Word data = base.ApplyRule(rewriteRule, input).SingleOrDefault();
						while (data != null)
						{
							output = data;
							i++;
							if (i > _delReapplications)
								break;
							data = base.ApplyRule(rewriteRule, data).SingleOrDefault();
						}
						if (output != null)
							return output.ToEnumerable();
					}
					break;

				case AnalysisReapplyType.SelfOpaquing:
					{
						Word output = null;
						Word data = base.ApplyRule(rewriteRule, input).SingleOrDefault();
						while (data != null)
						{
							if (rewriteRule.ApplicationMode == ApplicationMode.Iterative)
								RemoveSearchedValue(data);
							output = data;
							data = base.ApplyRule(rewriteRule, data).SingleOrDefault();
						}
						if (output != null)
							return output.ToEnumerable();
					}
					break;
			}

			return Enumerable.Empty<Word>();
		}

		private void RemoveSearchedValue(IData<ShapeNode> input)
		{
			foreach (Annotation<ShapeNode> ann in input.Annotations)
				ann.FeatureStruct.RemoveValue(HCFeatureSystem.Backtrack);
		}
	}
}
