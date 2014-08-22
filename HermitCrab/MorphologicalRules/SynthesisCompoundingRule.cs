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
	public class SynthesisCompoundingRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly CompoundingRule _rule;
		private readonly List<Tuple<Matcher<Word, ShapeNode>, Matcher<Word, ShapeNode>>> _subruleMatchers;

		public SynthesisCompoundingRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, CompoundingRule rule)
		{
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
			if (input.CurrentMorphologicalRule != _rule || input.GetApplicationCount(_rule) >= _rule.MaxApplicationCount)
			{
				return Enumerable.Empty<Word>();
			}

			if (!_rule.NonHeadRequiredSyntacticFeatureStruct.IsUnifiable(input.CurrentNonHead.SyntacticFeatureStruct, true))
			{
				if (_morpher.TraceManager.IsTracing)
					_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, -1, input, FailureReason.NonHeadRequiredSyntacticFeatureStruct);
				return Enumerable.Empty<Word>();
			}

			FeatureStruct syntacticFS;
			if (!_rule.HeadRequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
			{
				if (_morpher.TraceManager.IsTracing)
					_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, -1, input, FailureReason.HeadRequiredSyntacticFeatureStruct);
				return Enumerable.Empty<Word>();
			}

			var output = new List<Word>();
			for (int i = 0; i < _rule.Subrules.Count; i++)
			{
				if (_rule.Subrules[i].RequiredMprFeatures.Count > 0 && !_rule.Subrules[i].RequiredMprFeatures.IsMatch(input.MprFeatures))
				{
					if (_morpher.TraceManager.IsTracing)
						_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, i, input, FailureReason.RequiredMprFeatures);
					continue;
				}
				if (_rule.Subrules[i].ExcludedMprFeatures.Count > 0 && _rule.Subrules[i].ExcludedMprFeatures.IsMatch(input.MprFeatures))
				{
					if (_morpher.TraceManager.IsTracing)
						_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, i, input, FailureReason.ExcludedMprFeatures);
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
							if (_morpher.TraceManager.IsTracing)
								_morpher.TraceManager.Blocking(_rule, newWord);
							outWord = newWord;
						}
						else
						{
							outWord.Freeze();
						}

						if (_morpher.TraceManager.IsTracing)
							_morpher.TraceManager.MorphologicalRuleApplied(_rule, i, input, outWord);

						output.Add(outWord);
						break;
					}
				}

				if (_morpher.TraceManager.IsTracing)
					_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, i, input, FailureReason.PatternMismatch);
			}

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
				foreach (Allomorph allomorph in headMatch.Input.AllomorphsInMorphOrder)
				{
					List<ShapeNode> nodes;
					if (existingMorphNodes.TryGetValue(allomorph.ID, out nodes))
						output.MarkMorph(nodes, allomorph);
				}
			}

			output.MarkMorph(newMorphNodes, headMatch.Input.CurrentNonHead.RootAllomorph);

			return output;
		}
	}
}
