using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	internal class AnalysisStratumRule : RuleCascade<Word, ShapeNode>
	{
		private readonly IRule<Word, ShapeNode> _prulesRule;
		private readonly IRule<Word, ShapeNode> _templatesRule;
		private readonly Stratum _stratum;
		private readonly Morpher _morpher;

		public AnalysisStratumRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Stratum stratum)
			: base(CreateRules(spanFactory, morpher, stratum), stratum.MorphologicalRuleOrder, true, FreezableEqualityComparer<Word>.Instance)
		{
			_prulesRule = new RuleCascade<Word, ShapeNode>(stratum.PhonologicalRules.Select(prule => prule.CompileAnalysisRule(spanFactory, morpher)).Reverse(), stratum.PhonologicalRuleOrder);
			_templatesRule = new RuleCascade<Word, ShapeNode>(stratum.AffixTemplates.Select(template => template.CompileAnalysisRule(spanFactory, morpher)), FreezableEqualityComparer<Word>.Instance);
			_stratum = stratum;
			_morpher = morpher;
		}

		private static IEnumerable<IRule<Word, ShapeNode>> CreateRules(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Stratum stratum)
		{
			return stratum.MorphologicalRules.Select(mrule => mrule.CompileAnalysisRule(spanFactory, morpher)).Reverse();
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			if (_morpher.TraceRules.Contains(_stratum))
				input.CurrentTrace.Children.Add(new Trace(TraceType.StratumAnalysisInput, _stratum) {Input = input});

			input = input.DeepClone();
			input.Stratum = _stratum;

			_prulesRule.Apply(input);
			input.Freeze();

			var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Instance) {input};
			foreach (Word tempWord in _templatesRule.Apply(input).Concat(input))
			{
				output.UnionWith(base.Apply(tempWord));
				output.Add(tempWord);
			}

			if (_morpher.TraceRules.Contains(_stratum))
			{
				foreach (Word outWord in output)
					outWord.CurrentTrace.Children.Add(new Trace(TraceType.StratumAnalysisOutput, _stratum) {Output = outWord});
			}

			return output;
		}

		protected override IEnumerable<Word> ApplyRule(IRule<Word, ShapeNode> rule, int index, Word input)
		{
			foreach (Word outWord in rule.Apply(input))
			{
				if (RuleCascadeOrder == RuleCascadeOrder.Combination)
				{
					foreach (Word tempWord in _templatesRule.Apply(outWord))
						yield return tempWord;
				}

				yield return outWord;
			}
		}
	}
}
