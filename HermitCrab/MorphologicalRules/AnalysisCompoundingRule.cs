using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisCompoundingRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly CompoundingRule _rule;
		private readonly List<PatternRule<Word, ShapeNode>> _rules;

		public AnalysisCompoundingRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, CompoundingRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			_rules = new List<PatternRule<Word, ShapeNode>>();
			foreach (CompoundingSubrule sr in rule.Subrules)
			{
				_rules.Add(new PatternRule<Word, ShapeNode>(spanFactory, new AnalysisCompoundingSubruleRuleSpec(sr), ApplicationMode.Multiple,
					new MatcherSettings<ShapeNode>
					{
						Filter = ann => ann.Type() == HCFeatureSystem.Segment,
						AnchoredToStart = true,
						AnchoredToEnd = true,
						AllSubmatches = true
					}));
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
			foreach (PatternRule<Word, ShapeNode> rule in _rules)
			{
				var srOutput = new List<Word>();
				foreach (Word outWord in rule.Apply(input))
				{
					// for computational complexity reasons, we ensure that the non-head is a root, otherwise we assume it is not
					// a valid analysis and throw it away
					foreach (RootAllomorph allo in _morpher.SearchRootAllomorphs(_rule.Stratum, outWord.CurrentNonHead.Shape))
					{
						// check to see if this is a duplicate of another output analysis, this is not strictly necessary, but
						// it helps to reduce the search space
						bool add = true;
						for (int i = 0; i < srOutput.Count; i++)
						{
							if (outWord.Shape.Duplicates(srOutput[i].Shape) && allo == srOutput[i].CurrentNonHead.RootAllomorph)
							{
								if (outWord.Shape.Count > srOutput[i].Shape.Count)
									// if this is a duplicate and it is longer, then use this analysis and remove the previous one
									srOutput.RemoveAt(i);
								else
									// if it is shorter, then do not add it to the output list
									add = false;
								break;
							}
						}

						if (add)
						{
							Word newWord = outWord.DeepClone();
							newWord.CurrentNonHead.RootAllomorph = allo;
							srOutput.Add(newWord);
						}
					}
				}

				foreach (Word outWord in srOutput)
				{
					outWord.SyntacticFeatureStruct.Union(_rule.HeadRequiredSyntacticFeatureStruct);
					outWord.MorphologicalRuleUnapplied(_rule);

					if (_morpher.TraceRules.Contains(_rule))
					{
						var trace = new Trace(TraceType.MorphologicalRuleAnalysis, _rule) { Input = input.DeepClone(), Output = outWord.DeepClone() };
						outWord.CurrentTrace.Children.Add(trace);
						outWord.CurrentTrace = trace;
					}

					outWord.Freeze();
					output.Add(outWord);
				}
			}

			if (output.Count == 0 && _morpher.TraceRules.Contains(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, _rule) { Input = input.DeepClone() });

			return output;
		}
	}
}
