using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisAffixProcessRule : RuleCascade<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly AffixProcessRule _rule;

		public AnalysisAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, AffixProcessRule rule)
			: base(CreateRules(spanFactory, rule))
		{
			_morpher = morpher;
			_rule = rule;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory, AffixProcessRule rule)
		{
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				var transform = new AnalysisMorphologicalTransform(allo.Lhs, allo.Rhs);
				var ruleSpec = new DefaultPatternRuleSpec<Word, ShapeNode>(transform.Pattern,
					(PatternRule<Word, ShapeNode> patternRule, Match<Word, ShapeNode> match, out Word output) =>
						{
							output = transform.Unapply(match);
							return null;
						});
				yield return new PatternRule<Word, ShapeNode>(spanFactory, ruleSpec, ApplicationMode.Multiple,
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type() == HCFeatureSystem.Segment,
							AnchoredToStart = true,
							AnchoredToEnd = true
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
				outWord.SyntacticFeatureStruct.Union(_rule.RequiredSyntacticFeatureStruct);
				outWord.MorphologicalRuleUnapplied(_rule);

				if (_morpher.GetTraceRule(_rule))
				{
					var trace = new Trace(TraceType.MorphologicalRuleAnalysis, _rule) { Input = input.DeepClone(), Output = outWord.DeepClone() };
					outWord.CurrentTrace.Children.Add(trace);
					outWord.CurrentTrace = trace;
				}

				yield return outWord;
			}
		}
	}
}
