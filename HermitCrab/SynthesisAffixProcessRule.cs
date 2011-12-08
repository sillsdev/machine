using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class SynthesisAffixProcessRule : IRule<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly AffixProcessRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public SynthesisAffixProcessRule(SpanFactory<ShapeNode> spanFactory, AffixProcessRule rule)
		{
			_spanFactory = spanFactory;
			_rule = rule;
			_rules = new List<PatternRule<Word, ShapeNode>>();
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				var ruleSpec = new SynthesisAffixPatternRuleSpec(allo);
				_rules.Add(new PatternRule<Word, ShapeNode>(_spanFactory, ruleSpec,
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.BoundaryType, HCFeatureSystem.AnchorType),
							UseDefaultsForMatching = true
						}));
			}
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
				foreach (PatternRule<Word, ShapeNode> sr in _rules)
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

						AffixProcessAllomorph allo = ((SynthesisAffixPatternRuleSpec) sr.RuleSpec).Allomorph;
						if (allo.RequiredEnvironments == null && allo.ExcludedEnvironments == null)
							break;
					}
				}
			}

			output = outputList;
			return output != null;
		}
	}
}
