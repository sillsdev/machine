using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class AffixProcessSynthesisRule : IRule<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly AffixProcessRule _rule;
		private readonly List<SynthesisAffixPatternRule> _rules;

		public AffixProcessSynthesisRule(SpanFactory<ShapeNode> spanFactory, AffixProcessRule rule)
		{
			_spanFactory = spanFactory;
			_rule = rule;
			_rules = new List<SynthesisAffixPatternRule>();
		}

		public void AddAllomorph(AffixProcessAllomorph allomorph)
		{
			_rules.Add(new SynthesisAffixPatternRule(_spanFactory, allomorph));
		}

		public bool IsApplicable(Word input)
		{
			return input.CurrentRule == _rule && input.GetNumAppliesForMorphologicalRule(_rule) < _rule.MaxApplicationCount;
		}

		public bool Apply(Word input, out IEnumerable<Word> output)
		{
			List<Word> outputList = null;

			FeatureStruct syntacticFS;
			if (IsApplicable(input) && _rule.RequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
			{
				foreach (SynthesisAffixPatternRule sr in _rules)
				{
					IEnumerable<Word> outWords;
					if (sr.Apply(input, out outWords))
					{
						Word outWord = outWords.Single();

						outWord.SyntacticFeatureStruct = syntacticFS;
						outWord.SyntacticFeatureStruct.PriorityUnion(_rule.OutSyntacticFeatureStruct);

						outWord.MorphologicalRuleApplied(_rule);

						if (outputList == null)
							outputList = new List<Word>();
						outputList.Add(outWord);

						if (sr.Allomorph.RequiredEnvironments == null && sr.Allomorph.ExcludedEnvironments == null)
							break;
					}
				}
			}

			output = outputList;
			return output != null;
		}
	}
}
