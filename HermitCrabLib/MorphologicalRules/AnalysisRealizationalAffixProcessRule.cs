using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisRealizationalAffixProcessRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly RealizationalAffixProcessRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public AnalysisRealizationalAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, RealizationalAffixProcessRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			_rules = new List<PatternRule<Word, ShapeNode>>();
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				_rules.Add(new MultiplePatternRule<Word, ShapeNode>(spanFactory, new AnalysisAffixProcessAllomorphRuleSpec(allo),
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type() == HCFeatureSystem.Segment,
							MatchingMethod = MatchingMethod.Unification,
							AnchoredToStart = true,
							AnchoredToEnd = true,
							AllSubmatches = true
						}));
			}
		}

		public IEnumerable<Word> Apply(Word input)
		{
			FeatureStruct realFS;
			if (!_rule.RealizationalFeatureStruct.Unify(input.RealizationalFeatureStruct, out realFS))
				return Enumerable.Empty<Word>();
			var output = new List<Word>();
			foreach (PatternRule<Word, ShapeNode> rule in _rules)
			{
				foreach (Word outWord in rule.Apply(input).RemoveDuplicates())
				{
					outWord.RealizationalFeatureStruct = realFS;
					outWord.MorphologicalRuleUnapplied(_rule, true);
					
					outWord.Freeze();
					if (_morpher.TraceRules.Contains(_rule))
					{
						var trace = new Trace(TraceType.MorphologicalRuleAnalysis, _rule) {Input = input, Output = outWord};
						outWord.CurrentTrace.Children.Add(trace);
						outWord.CurrentTrace = trace;
					}
					output.Add(outWord);
				}
			}
			if (output.Count == 0 && _morpher.TraceRules.Contains(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, _rule) {Input = input});
			return output;
		}
	}
}
