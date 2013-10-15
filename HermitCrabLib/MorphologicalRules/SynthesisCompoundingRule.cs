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
	public class SynthesisCompoundingRule : IRule<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Morpher _morpher;
		private readonly CompoundingRule _rule;
		private readonly List<Tuple<Matcher<Word, ShapeNode>, Matcher<Word, ShapeNode>>> _subruleMatchers;

		public SynthesisCompoundingRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, CompoundingRule rule)
		{
			_spanFactory = spanFactory;
			_morpher = morpher;
			_rule = rule;
			_subruleMatchers = new List<Tuple<Matcher<Word, ShapeNode>, Matcher<Word, ShapeNode>>>();
			foreach (CompoundingSubrule sr in rule.Subrules)
				_subruleMatchers.Add(Tuple.Create(BuildMatcher(spanFactory, sr.HeadLhs), BuildMatcher(spanFactory, sr.NonHeadLhs)));
		}

		private Matcher<Word, ShapeNode> BuildMatcher(SpanFactory<ShapeNode> spanFactory, IEnumerable<Pattern<Word, ShapeNode>> lhs)
		{
			var pattern = new Pattern<Word, ShapeNode>();
			foreach (Pattern<Word, ShapeNode> part in lhs)
				pattern.Children.Add(new Group<Word, ShapeNode>(part.Name, part.Children.DeepClone()));

			return new Matcher<Word, ShapeNode>(spanFactory, pattern,
				new MatcherSettings<ShapeNode>
					{
						Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary) && !ann.IsDeleted(),
						AnchoredToStart = true,
						AnchoredToEnd = true
					});
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (input.CurrentMorphologicalRule != _rule || input.GetApplicationCount(_rule) >= _rule.MaxApplicationCount
				|| !_rule.NonHeadRequiredSyntacticFeatureStruct.IsUnifiable(input.CurrentNonHead.SyntacticFeatureStruct, true))
			{
				return Enumerable.Empty<Word>();
			}

			FeatureStruct syntacticFS;
			if (!_rule.HeadRequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
				return Enumerable.Empty<Word>();

			var output = new List<Word>();
			for (int i = 0; i < _rule.Subrules.Count; i++)
			{
				if (_rule.Subrules[i].RequiredMprFeatures.Count > 0 && !_rule.Subrules[i].RequiredMprFeatures.IsMatch(input.MprFeatures)
					|| (_rule.Subrules[i].ExcludedMprFeatures.Count > 0 && _rule.Subrules[i].ExcludedMprFeatures.IsMatch(input.MprFeatures)))
				{
					continue;
				}

				Match<Word, ShapeNode> headMatch = _subruleMatchers[i].Item1.Match(input);
				if (headMatch.Success)
				{
					Match<Word, ShapeNode> nonHeadMatch = _subruleMatchers[i].Item2.Match(input.CurrentNonHead);
					if (nonHeadMatch.Success)
					{
						Word outWord = ApplySubrule(_rule.Subrules[i], headMatch, nonHeadMatch);

						outWord.MprFeatures.AddOutput(_rule.Subrules[i].OutMprFeatures);

						outWord.SyntacticFeatureStruct = syntacticFS;
						outWord.SyntacticFeatureStruct.PriorityUnion(_rule.OutSyntacticFeatureStruct);

						foreach (Feature feature in _rule.ObligatorySyntacticFeatures)
							outWord.ObligatorySyntacticFeatures.Add(feature);

						outWord.CurrentMorphologicalRuleApplied();
						outWord.CurrentNonHeadApplied();

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
						break;
					}
				}
			}

			if (output.Count == 0 && _morpher.TraceRules.Contains(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleSynthesis, _rule) {Input = input});

			return output;
		}

		private Word ApplySubrule(CompoundingSubrule sr, Match<Word, ShapeNode> headMatch, Match<Word, ShapeNode> nonHeadMatch)
		{
			// TODO: unify the variable bindings from the head and non-head matches
			Word output = headMatch.Input.DeepClone();
			output.Shape.Clear();

			var existingMorphNodes = new Dictionary<string, List<ShapeNode>>();
			var newMorphNodes = new List<ShapeNode>();
			foreach (MorphologicalOutputAction outputAction in sr.Rhs)
			{
				if (outputAction.PartName != null && nonHeadMatch.GroupCaptures.Captured(outputAction.PartName))
				{
					newMorphNodes.AddRange(outputAction.Apply(nonHeadMatch, output).Select(mapping => mapping.Item2));
				}
				else
				{
					foreach (Tuple<ShapeNode, ShapeNode> mapping in outputAction.Apply(headMatch, output))
					{
						if (mapping.Item1 != null && mapping.Item1.Annotation.Parent != null)
						{
							var allomorphID = (string) mapping.Item1.Annotation.Parent.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
							existingMorphNodes.GetValue(allomorphID, () => new List<ShapeNode>()).Add(mapping.Item2);
						}
					}
				}
			}

			if (existingMorphNodes.Count > 0)
			{
				foreach (Annotation<ShapeNode> morph in headMatch.Input.Morphs)
				{
					List<ShapeNode> nodes;
					if (existingMorphNodes.TryGetValue((string) morph.FeatureStruct.GetValue(HCFeatureSystem.Allomorph), out nodes))
						MarkMorph(output, nodes, morph.FeatureStruct.DeepClone());
				}
			}

			if (newMorphNodes.Count > 0)
			{
				FeatureStruct fs = FeatureStruct.New()
					.Symbol(HCFeatureSystem.Morph)
					.Feature(HCFeatureSystem.Allomorph).EqualTo(headMatch.Input.CurrentNonHead.RootAllomorph.ID).Value;
				MarkMorph(output, newMorphNodes, fs);
			}
			output.Allomorphs.Add(headMatch.Input.CurrentNonHead.RootAllomorph);

			return output;
		}

		private void MarkMorph(Word output, List<ShapeNode> nodes, FeatureStruct fs)
		{
			if (nodes.Count == 0)
				return;

			var ann = new Annotation<ShapeNode>(_spanFactory.Create(nodes[0], nodes[nodes.Count - 1]), fs);
			ann.Children.AddRange(nodes.Select(n => n.Annotation));
			output.Annotations.Add(ann, false);
		}
	}
}
