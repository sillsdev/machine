using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisRealizationalAffixProcessRule : RuleCascade<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly RealizationalAffixProcessRule _rule;

		public AnalysisRealizationalAffixProcessRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, RealizationalAffixProcessRule rule)
			: base(CreateRules(spanFactory, rule))
		{
			_morpher = morpher;
			_rule = rule;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory, RealizationalAffixProcessRule rule)
		{
			foreach (AffixProcessAllomorph allo in rule.Allomorphs)
			{
				yield return new PatternRule<Word, ShapeNode>(spanFactory, new AnalysisAffixProcessAllomorphRuleSpec(allo), ApplicationMode.Multiple,
					new MatcherSettings<ShapeNode>
						{
							Filter = ann => ann.Type() == HCFeatureSystem.Segment,
							AnchoredToStart = true,
							AnchoredToEnd = true,
							AllSubmatches = true
						});
			}
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			FeatureStruct realFS;
			if (!_rule.RealizationalFeatureStruct.Unify(input.RealizationalFeatureStruct, out realFS))
				return Enumerable.Empty<Word>();
			List<Word> output = base.Apply(input).ToList();
			foreach (Word result in output)
				result.RealizationalFeatureStruct = realFS;
			if (output.Count == 0 && _morpher.GetTraceRule(_rule))
				input.CurrentTrace.Children.Add(new Trace(TraceType.MorphologicalRuleAnalysis, _rule) { Input = input.DeepClone() });
			return output;
		}

		protected override IEnumerable<Word> ApplyRule(IRule<Word, ShapeNode> rule, int index, Word input)
		{
			foreach (Word outWord in rule.Apply(input))
			{
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
