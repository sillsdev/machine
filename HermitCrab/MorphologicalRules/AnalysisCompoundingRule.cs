using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisCompoundingRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly CompoundingRule _rule;
		private readonly List<Tuple<PatternRule<Word, ShapeNode>, PatternRule<Word, ShapeNode>>> _patternRules; 

		public AnalysisCompoundingRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, CompoundingRule rule)
		{
			_morpher = morpher;
			_rule = rule;
			_patternRules = new List<Tuple<PatternRule<Word, ShapeNode>, PatternRule<Word, ShapeNode>>>();
			foreach (CompoundingSubrule sr in rule.Subrules)
			{
				var headRule = new PatternRule<Word, ShapeNode>(spanFactory, new AnalysisCompoundingRuleSpec(sr), ApplicationMode.Multiple,
					new MatcherSettings<ShapeNode>
						{
							Direction = sr.Headedness == Headedness.LeftHeaded ? Direction.LeftToRight : Direction.RightToLeft,
							Filter = ann => ann.Type() == HCFeatureSystem.Segment,
							AnchoredToStart = true
						});
				var nonHeadRule = new PatternRule<Word, ShapeNode>(spanFactory,
					new AnalysisMorphologicalRuleSpec(sr.Headedness == Headedness.LeftHeaded ? sr.RightLhs : sr.LeftLhs, sr.Headedness == Headedness.LeftHeaded ? sr.RightRhs : sr.LeftRhs),
					ApplicationMode.Multiple, new MatcherSettings<ShapeNode>
												{
													Filter = ann => ann.Type() == HCFeatureSystem.Segment,
													AnchoredToStart = true,
													AnchoredToEnd = true
												});
				_patternRules.Add(Tuple.Create(headRule, nonHeadRule));
			}
		}

		public bool IsApplicable(Word input)
		{
			return input.GetUnapplicationCount(_rule) < _rule.MaxApplicationCount
				&& _rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct);
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var output = new List<Word>();
			foreach (Tuple<PatternRule<Word, ShapeNode>, PatternRule<Word, ShapeNode>> r in _patternRules)
			{
				foreach (Word headWord in r.Item1.Apply(input))
				{
					foreach (Word nonHeadWord in r.Item2.Apply(headWord.CurrentNonHead))
					{
						foreach (RootAllomorph allo in _morpher.SearchRootAllomorphs(_rule.Stratum, nonHeadWord.Shape))
						{
							Word newWord = headWord.DeepClone();
							newWord.CurrentNonHead.RootAllomorph = allo;

							newWord.SyntacticFeatureStruct.Union(_rule.HeadRequiredSyntacticFeatureStruct);
							newWord.MorphologicalRuleUnapplied(_rule);

							if (_morpher.GetTraceRule(_rule))
							{
								var trace = new Trace(TraceType.MorphologicalRuleAnalysis, _rule) { Input = input.DeepClone(), Output = newWord.DeepClone() };
								newWord.CurrentTrace.Children.Add(trace);
								newWord.CurrentTrace = trace;
							}
							
							output.Add(newWord);
						}
					}
				}
			}

			if (output.Count == 0 && _morpher.GetTraceRule(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, _rule) { Input = input.DeepClone() });

			return output;
		}
	}
}
