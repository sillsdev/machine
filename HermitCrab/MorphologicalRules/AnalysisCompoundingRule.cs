using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisCompoundingRule : RuleCascade<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly CompoundingRule _rule;

		public AnalysisCompoundingRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, CompoundingRule rule)
			: base(CreateRules(spanFactory, rule))
		{
			_morpher = morpher;
			_rule = rule;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory, CompoundingRule rule)
		{
			foreach (CompoundingSubrule sr in rule.Subrules)
			{
				yield return new PatternRule<Word, ShapeNode>(spanFactory, new AnalysisCompoundingSubruleRuleSpec(sr), ApplicationMode.Multiple,
					new MatcherSettings<ShapeNode>
					{
						Filter = ann => ann.Type() == HCFeatureSystem.Segment,
						AnchoredToStart = true,
						AnchoredToEnd = true,
						AllSubmatches = true
					});
			}
		}

		public override bool IsApplicable(Word input)
		{
			return input.GetUnapplicationCount(_rule) < _rule.MaxApplicationCount
				&& _rule.OutSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct);
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			List<Word> output = base.Apply(input).ToList();
			if (output.Count == 0 && _morpher.GetTraceRule(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, _rule) { Input = input.DeepClone() });
			return output;
		}

		protected override IEnumerable<Word> ApplyRule(IRule<Word, ShapeNode> rule, int index, Word input)
		{
			foreach (Word outWord in rule.Apply(input))
			{
				foreach (RootAllomorph allo in _morpher.SearchRootAllomorphs(_rule.Stratum, outWord.CurrentNonHead.Shape))
				{
					Word newWord = outWord.DeepClone();
					newWord.CurrentNonHead.RootAllomorph = allo;

					newWord.SyntacticFeatureStruct.Union(_rule.HeadRequiredSyntacticFeatureStruct);
					newWord.MorphologicalRuleUnapplied(_rule);

					if (_morpher.GetTraceRule(_rule))
					{
						var trace = new Trace(TraceType.MorphologicalRuleAnalysis, _rule) { Input = input.DeepClone(), Output = newWord.DeepClone() };
						newWord.CurrentTrace.Children.Add(trace);
						newWord.CurrentTrace = trace;
					}

					yield return newWord;
				}
			}
		}
	}
}
