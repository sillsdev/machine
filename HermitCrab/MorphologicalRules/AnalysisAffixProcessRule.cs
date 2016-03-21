using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisAffixProcessRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly AffixProcessRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public AnalysisAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixProcessRule rule)
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
			if (!_morpher.RuleSelector(_rule))
				return Enumerable.Empty<Word>();

			if (input.GetUnapplicationCount(_rule) >= _rule.MaxApplicationCount
				|| !_rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct))
			{
				return Enumerable.Empty<Word>();
			}

			var output = new List<Word>();
			for (int i = 0; i < _rules.Count; i++)
			{
				bool unapplied = false;
				foreach (Word outWord in _rules[i].Apply(input).RemoveDuplicates())
				{
					if (!_rule.RequiredSyntacticFeatureStruct.IsEmpty)
						outWord.SyntacticFeatureStruct.Add(_rule.RequiredSyntacticFeatureStruct);
					else if (_rule.OutSyntacticFeatureStruct.IsEmpty)
						outWord.SyntacticFeatureStruct.Clear();
					outWord.MorphologicalRuleUnapplied(_rule, false);
					outWord.Freeze();
					if (_morpher.TraceManager.IsTracing)
						_morpher.TraceManager.MorphologicalRuleUnapplied(_rule, i, input, outWord);
					output.Add(outWord);
					unapplied = true;
				}

				if (_morpher.TraceManager.IsTracing && !unapplied)
					_morpher.TraceManager.MorphologicalRuleNotUnapplied(_rule, i, input);
			}
			return output;
		}
	}
}
