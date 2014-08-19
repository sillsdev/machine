using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class SynthesisAffixProcessRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly AffixProcessRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public SynthesisAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixProcessRule rule)
		{
			_morpher = morpher;
			_rule = rule;
			_rules = new List<PatternRule<Word, ShapeNode>>();
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				var ruleSpec = new SynthesisAffixProcessAllomorphRuleSpec(allo);
				_rules.Add(new PatternRule<Word, ShapeNode>(spanFactory, ruleSpec,
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary) && !ann.IsDeleted(),
							AnchoredToStart = true,
							AnchoredToEnd = true
						}));
			}
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (input.CurrentMorphologicalRule != _rule || input.GetApplicationCount(_rule) >= _rule.MaxApplicationCount)
				return Enumerable.Empty<Word>();

			if (_rule.RequiredStemName != null && _rule.RequiredStemName != input.RootAllomorph.StemName)
			{
				_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, input, FailureReason.StemName);
				return Enumerable.Empty<Word>();
			}

			FeatureStruct syntacticFS;
			if (!_rule.RequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
			{
				_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, input, FailureReason.RequiredSyntacticFeatureStruct);
				return Enumerable.Empty<Word>();
			}

			var output = new List<Word>();
			for (int i = 0; i < _rules.Count; i++)
			{
				Word outWord = _rules[i].Apply(input).SingleOrDefault();
				if (outWord != null)
				{
					outWord.SyntacticFeatureStruct = syntacticFS;
					outWord.SyntacticFeatureStruct.PriorityUnion(_rule.OutSyntacticFeatureStruct);

					foreach (Feature obligFeature in _rule.ObligatorySyntacticFeatures)
						outWord.ObligatorySyntacticFeatures.Add(obligFeature);

					outWord.CurrentMorphologicalRuleApplied();

					Word newWord;
					if (_rule.Blockable && outWord.CheckBlocking(out newWord))
					{
						_morpher.TraceManager.Blocking(_rule, newWord);
						outWord = newWord;
					}
					else
					{
						outWord.Freeze();
					}

					AffixProcessAllomorph allo = _rule.Allomorphs[i];
					_morpher.TraceManager.MorphologicalRuleApplied(_rule, input, outWord, allo);
					output.Add(outWord);

					// return all word syntheses that match subrules that are constrained by environments,
					// HC violates the disjunctive property of allomorphs here because it cannot check the
					// environmental constraints until it has a surface form, we will enforce the disjunctive
					// property of allomorphs at that time

					// HC also checks for free fluctuation, if the next subrule has the same constraints, we
					// do not treat them as disjunctive
					if ((i != _rule.Allomorphs.Count - 1 && !allo.FreeFluctuatesWith(_rule.Allomorphs[i + 1]))
						&& allo.RequiredEnvironments.Count == 0 && allo.ExcludedEnvironments.Count == 0
						&& allo.RequiredSyntacticFeatureStruct.IsEmpty)
					{
						break;
					}
				}
			}

			if (output.Count == 0)
				_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, input, FailureReason.SubruleMismatch);

			return output;
		}
	}
}
