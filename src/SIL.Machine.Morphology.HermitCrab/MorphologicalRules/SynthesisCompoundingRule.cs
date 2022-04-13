using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	public class SynthesisCompoundingRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly CompoundingRule _rule;
		private readonly List<Tuple<Matcher<Word, ShapeNode>, Matcher<Word, ShapeNode>>> _subruleMatchers;

		public SynthesisCompoundingRule(Morpher morpher, CompoundingRule rule)
		{
			_morpher = morpher;
			_rule = rule;
			_subruleMatchers = new List<Tuple<Matcher<Word, ShapeNode>, Matcher<Word, ShapeNode>>>();
			foreach (CompoundingSubrule sr in rule.Subrules)
				_subruleMatchers.Add(Tuple.Create(BuildMatcher(sr.HeadLhs), BuildMatcher(sr.NonHeadLhs)));
		}

		private Matcher<Word, ShapeNode> BuildMatcher(IEnumerable<Pattern<Word, ShapeNode>> lhs)
		{
			var pattern = new Pattern<Word, ShapeNode>();
			foreach (Pattern<Word, ShapeNode> part in lhs)
				pattern.Children.Add(new Group<Word, ShapeNode>(part.Name, part.Children.CloneItems()));

			return new Matcher<Word, ShapeNode>(pattern,
				new MatcherSettings<ShapeNode>
				{
					Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary)
						&& !ann.IsDeleted(),
					AnchoredToStart = true,
					AnchoredToEnd = true
				});
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (!input.IsMorphologicalRuleApplicable(_rule))
				return Enumerable.Empty<Word>();

			if (input.GetApplicationCount(_rule) >= _rule.MaxApplicationCount)
			{
				if (_morpher.TraceManager.IsTracing)
				{
					_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, -1, input,
						FailureReason.MaxApplicationCount, _rule.MaxApplicationCount);
				}
				return Enumerable.Empty<Word>();
			}

			if ((input.IsLastAppliedRuleFinal ?? false) && !input.IsPartial)
			{
				if (_morpher.TraceManager.IsTracing)
				{
					_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, -1, input,
						FailureReason.NonPartialRuleProhibitedAfterFinalTemplate, null);
				}
				return Enumerable.Empty<Word>();
			}

			if (!_rule.NonHeadRequiredSyntacticFeatureStruct.IsUnifiable(input.CurrentNonHead.SyntacticFeatureStruct,
				true))
			{
				if (_morpher.TraceManager.IsTracing)
				{
					_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, -1, input,
						FailureReason.NonHeadRequiredSyntacticFeatureStruct,
						_rule.NonHeadRequiredSyntacticFeatureStruct);
				}
				return Enumerable.Empty<Word>();
			}

			FeatureStruct syntacticFS;
			if (!_rule.HeadRequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
			{
				if (_morpher.TraceManager.IsTracing)
				{
					_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, -1, input,
						FailureReason.HeadRequiredSyntacticFeatureStruct, _rule.HeadRequiredSyntacticFeatureStruct);
				}
				return Enumerable.Empty<Word>();
			}

			var output = new List<Word>();
			for (int i = 0; i < _rule.Subrules.Count; i++)
			{
				MprFeatureGroup group;
				if (_rule.Subrules[i].RequiredMprFeatures.Count > 0
					&& !_rule.Subrules[i].RequiredMprFeatures.IsMatchRequired(input.MprFeatures, out group))
				{
					if (_morpher.TraceManager.IsTracing)
					{
						_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, i, input,
							FailureReason.RequiredMprFeatures, group);
					}
					continue;
				}
				if (_rule.Subrules[i].ExcludedMprFeatures.Count > 0
					&& !_rule.Subrules[i].ExcludedMprFeatures.IsMatchExcluded(input.MprFeatures, out group))
				{
					if (_morpher.TraceManager.IsTracing)
					{
						_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, i, input,
							FailureReason.ExcludedMprFeatures, group);
					}
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

						outWord.IsLastAppliedRuleFinal = null;

						outWord.MorphologicalRuleApplied(_rule);

						Word newWord;
						if (_rule.Blockable && outWord.CheckBlocking(out newWord))
						{
							if (_morpher.TraceManager.IsTracing)
								_morpher.TraceManager.Blocked(_rule, newWord);
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
					if (_morpher.TraceManager.IsTracing)
					{
						_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, i, input, FailureReason.NonHeadPattern,
							null);
					}
				}
				else if (_morpher.TraceManager.IsTracing)
				{
					_morpher.TraceManager.MorphologicalRuleNotApplied(_rule, i, input, FailureReason.HeadPattern, null);
				}
			}

			return output;
		}

		private Word ApplySubrule(CompoundingSubrule sr, Match<Word, ShapeNode> headMatch,
			Match<Word, ShapeNode> nonHeadMatch)
		{
			// TODO: unify the variable bindings from the head and non-head matches
			Word output = headMatch.Input.Clone();
			output.Shape.Clear();

			var existingMorphNodes = new Dictionary<Annotation<ShapeNode>, List<ShapeNode>>();
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
							Annotation<ShapeNode> morph = mapping.Item1.Annotation.Parent;
							existingMorphNodes.GetOrCreate(morph, () => new List<ShapeNode>()).Add(mapping.Item2);
						}
					}
				}
			}

			if (existingMorphNodes.Count > 0)
			{
				foreach (Annotation<ShapeNode> inputMorph in headMatch.Input.Morphs)
				{
					if (existingMorphNodes.TryGetValue(inputMorph, out List<ShapeNode> nodes))
					{
						Allomorph allomorph = headMatch.Input.GetAllomorph(inputMorph);
						string morphID = (string)inputMorph.FeatureStruct.GetValue(HCFeatureSystem.MorphID);
						output.MarkMorph(nodes, allomorph, morphID);
					}
				}
			}

			output.MarkMorph(newMorphNodes, headMatch.Input.CurrentNonHead.RootAllomorph, Word.RootMorphID);

			return output;
		}
	}
}
