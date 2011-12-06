using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class AffixProcessAnalysisRule : RuleCascade<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly AffixProcessRule _rule;

		public AffixProcessAnalysisRule(SpanFactory<ShapeNode> spanFactory, AffixProcessRule rule)
		{
			_spanFactory = spanFactory;
			_rule = rule;
		}

		public void AddAllomorph(AffixProcessAllomorph allomorph)
		{
			InsertRuleInternal(Rules.Count, new AnalysisAffixPatternRule(_spanFactory, allomorph));
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
