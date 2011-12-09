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

		public override IEnumerable<Word> Apply(Word input)
		{
			IEnumerable<Word> output = base.Apply(input);
			// TODO: add trace record here
			return output;
		}

		protected override IEnumerable<Word> ApplyRule(IRule<Word, ShapeNode> rule, Word input)
		{
			foreach (Word outWord in CallBaseApplyRule(rule, input))
			{
				outWord.SyntacticFeatureStruct.Merge(_rule.RequiredSyntacticFeatureStruct);
				outWord.MorphologicalRuleUnapplied(_rule);
				yield return outWord;
			}
		}

		private IEnumerable<Word> CallBaseApplyRule(IRule<Word, ShapeNode> rule, Word input)
		{
			return base.ApplyRule(rule, input);
		}
	}
}
