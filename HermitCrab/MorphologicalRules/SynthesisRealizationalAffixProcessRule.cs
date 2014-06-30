using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class SynthesisRealizationalAffixProcessRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly RealizationalAffixProcessRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public SynthesisRealizationalAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, RealizationalAffixProcessRule rule)
		{
			_morpher = morpher;
			_rule = rule;
			_rules = new List<PatternRule<Word, ShapeNode>>();
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				_rules.Add(new PatternRule<Word, ShapeNode>(spanFactory, new SynthesisAffixProcessAllomorphRuleSpec(allo),
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
			if (!_morpher.RuleSelector(_rule))
				return Enumerable.Empty<Word>();

			if (!_rule.RealizationalFeatureStruct.Subsumes(input.RealizationalFeatureStruct))
				return Enumerable.Empty<Word>();

			if (!_rule.RealizationalFeatureStruct.IsEmpty && IsBlocked(_rule.RealizationalFeatureStruct, input.SyntacticFeatureStruct, new HashSet<Tuple<FeatureStruct, FeatureStruct>>()))
				return Enumerable.Empty<Word>();

			FeatureStruct syntacticFS;
			if (!_rule.RequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
				return Enumerable.Empty<Word>();

			var output = new List<Word>();
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

					if (allo.RequiredEnvironments == null && allo.ExcludedEnvironments == null)
						break;
				}
			}

			if (output.Count == 0)
				_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, input);
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
