using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.MorphologicalRules
{
	public class AnalysisCompoundingRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly CompoundingRule _rule;
		private readonly List<IRule<Word, ShapeNode>> _rules;

		public AnalysisCompoundingRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, CompoundingRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			_rules = new List<IRule<Word, ShapeNode>>();
			foreach (CompoundingSubrule sr in rule.Subrules)
			{
				_rules.Add(new MultiplePatternRule<Word, ShapeNode>(spanFactory, new AnalysisCompoundingSubruleRuleSpec(sr),
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

			if (input.NonHeadCount + 1 >= _morpher.MaxStemCount || input.GetUnapplicationCount(_rule) >= _rule.MaxApplicationCount
				|| !_rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct))
			{
				return Enumerable.Empty<Word>();
			}

			var output = new List<Word>();
			for (int i = 0; i < _rules.Count; i++)
			{
				var srOutput = new List<Word>();
				foreach (Word outWord in _rules[i].Apply(input))
				{
					// for computational complexity reasons, we ensure that the non-head is a root, otherwise we assume it is not
					// a valid analysis and throw it away
					foreach (RootAllomorph allo in _morpher.SearchRootAllomorphs(_rule.Stratum, outWord.CurrentNonHead.Shape))
					{
						// check to see if this is a duplicate of another output analysis, this is not strictly necessary, but
						// it helps to reduce the search space
						bool add = true;
						for (int j = 0; j < srOutput.Count; j++)
						{
							if (outWord.Shape.Duplicates(srOutput[j].Shape) && allo == srOutput[j].CurrentNonHead.RootAllomorph)
							{
								if (outWord.Shape.Count > srOutput[j].Shape.Count)
									// if this is a duplicate and it is longer, then use this analysis and remove the previous one
									srOutput.RemoveAt(j);
								else
									// if it is shorter, then do not add it to the output list
									add = false;
								break;
							}
						}

						if (add)
						{
							Word newWord = outWord.Clone();
							newWord.CurrentNonHead.RootAllomorph = allo;
							srOutput.Add(newWord);
						}
					}
				}

				bool unapplied = false;
				foreach (Word outWord in srOutput)
				{
					if (!_rule.HeadRequiredSyntacticFeatureStruct.IsEmpty)
						outWord.SyntacticFeatureStruct.Add(_rule.HeadRequiredSyntacticFeatureStruct);
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
