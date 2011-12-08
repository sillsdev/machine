using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class AnalysisAffixProcessRule : RuleCascade<Word, ShapeNode>
	{
		private readonly AffixProcessRule _rule;

		public AnalysisAffixProcessRule(SpanFactory<ShapeNode> spanFactory, AffixProcessRule rule)
			: base(CreateRules(spanFactory, rule))
		{
			_rule = rule;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory, AffixProcessRule rule)
		{
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				var ruleSpec = new AnalysisAffixPatternRuleSpec(allo);
				yield return new PatternRule<Word, ShapeNode>(spanFactory, ruleSpec, ApplicationMode.Multiple,
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.AnchorType)
						});
			}
		}

		public override bool IsApplicable(Word input)
		{
			return input.GetNumUnappliesForMorphologicalRule(_rule) < _rule.MaxApplicationCount
				&& _rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct);
		}

		public override bool Apply(Word input, out IEnumerable<Word> output)
		{
			bool result = base.Apply(input, out output);
			// TODO: add trace record here
			return result;
		}

		protected override bool ApplyRule(IRule<Word, ShapeNode> rule, Word input, out IEnumerable<Word> output)
		{
			if (base.ApplyRule(rule, input, out output))
			{
				foreach (Word outWord in output)
				{
					outWord.SyntacticFeatureStruct.Merge(_rule.RequiredSyntacticFeatureStruct);
					outWord.MorphologicalRuleUnapplied(_rule);
				}

				return true;
			}

			return false;
		}
	}
}
