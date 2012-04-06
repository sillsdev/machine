using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class SynthesisAffixProcessRule : IRule<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Morpher _morpher;
		private readonly AffixProcessRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public SynthesisAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixProcessRule rule)
		{
			_spanFactory = spanFactory;
			_morpher = morpher;
			_rule = rule;
			_rules = new List<PatternRule<Word, ShapeNode>>();
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				var ruleSpec = new SynthesisAffixProcessAllomorphRuleSpec(allo);
				_rules.Add(new PatternRule<Word, ShapeNode>(_spanFactory, ruleSpec,
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary),
							UseDefaults = true,
							AnchoredToStart = true,
							AnchoredToEnd = true
						}));
			}
		}

		public bool IsApplicable(Word input)
		{
			return input.CurrentMorphologicalRule == _rule && input.GetApplicationCount(_rule) < _rule.MaxApplicationCount;
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var output = new List<Word>();
			FeatureStruct syntacticFS;
			if (_rule.RequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
			{
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
							if (_morpher.TraceBlocking)
								newWord.CurrentTrace.Children.Add(new Trace(TraceType.Blocking, _rule) {Output = newWord});
							outWord = newWord;
						}
						else
						{
							outWord.Freeze();
						}

						if (_morpher.TraceRules.Contains(_rule))
						{
							var trace = new Trace(TraceType.MorphologicalRuleSynthesis, _rule) {Input = input, Output = outWord};
							outWord.CurrentTrace.Children.Add(trace);
							outWord.CurrentTrace = trace;
						}
						output.Add(outWord);

						// return all word syntheses that match subrules that are constrained by environments,
						// HC violates the disjunctive property of allomorphs here because it cannot check the
						// environmental constraints until it has a surface form, we will enforce the disjunctive
						// property of allomorphs at that time

						// HC also checks for free fluctuation, if the next subrule has the same constraints, we
						// do not treat them as disjunctive
						AffixProcessAllomorph allo = _rule.Allomorphs[i];
						if ((i != _rule.Allomorphs.Count - 1 && !allo.ConstraintsEqual(_rule.Allomorphs[i + 1]))
							&& allo.RequiredEnvironments.Count == 0 && allo.ExcludedEnvironments.Count == 0)
						{
							break;
						}
					}
				}
			}

			if (output.Count == 0 && _morpher.TraceRules.Contains(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleSynthesis, _rule) {Input = input});

			return output;
		}
	}
}
