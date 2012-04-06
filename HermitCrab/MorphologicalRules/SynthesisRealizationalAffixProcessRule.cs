using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class SynthesisRealizationalAffixProcessRule : IRule<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Morpher _morpher;
		private readonly RealizationalAffixProcessRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public SynthesisRealizationalAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, RealizationalAffixProcessRule rule)
		{
			_spanFactory = spanFactory;
			_morpher = morpher;
			_rule = rule;
			_rules = new List<PatternRule<Word, ShapeNode>>();
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				_rules.Add(new PatternRule<Word, ShapeNode>(_spanFactory, new SynthesisAffixProcessAllomorphRuleSpec(allo),
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
			return _rule.RealizationalFeatureStruct.Subsumes(input.RealizationalFeatureStruct);
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var output = new List<Word>();
			FeatureStruct syntacticFS;
			if ((_rule.RealizationalFeatureStruct.IsEmpty || !IsBlocked(_rule.RealizationalFeatureStruct, input.SyntacticFeatureStruct, new HashSet<Tuple<FeatureStruct, FeatureStruct>>()))
				&& _rule.RequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
			{
				for (int i = 0; i < _rules.Count; i++)
				{
					Word outWord = _rules[i].Apply(input).SingleOrDefault();
					if (outWord != null)
					{
						outWord.SyntacticFeatureStruct = syntacticFS;
						outWord.SyntacticFeatureStruct.PriorityUnion(_rule.RealizationalFeatureStruct);
						outWord.MorphologicalRuleApplied(_rule);

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

						AffixProcessAllomorph allo = _rule.Allomorphs[i];
						if (allo.RequiredEnvironments == null && allo.ExcludedEnvironments == null)
							break;
					}
				}
			}

			if (output.Count == 0 && _morpher.TraceRules.Contains(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleSynthesis, _rule) {Input = input});
			return output;
		}

		private bool IsBlocked(FeatureStruct realizationalFS, FeatureStruct syntacticFS, HashSet<Tuple<FeatureStruct, FeatureStruct>> visited)
		{
			Tuple<FeatureStruct, FeatureStruct> pair = Tuple.Create(realizationalFS, syntacticFS);
			if (visited.Contains(pair))
				return true;

			visited.Add(pair);

			foreach (Feature f in realizationalFS.Features)
			{
				if (!syntacticFS.ContainsFeature(f))
					return false;

				var cf = f as ComplexFeature;
				if (cf != null)
				{
					FeatureStruct realFS = realizationalFS.GetValue(cf);
					FeatureStruct synFS = syntacticFS.GetValue(cf);
					if (!IsBlocked(realFS, synFS, visited))
						return false;
				}
			}

			return true;
		}
	}
}
