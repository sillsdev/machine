using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	internal class AnalysisStratumRule : IRule<Word, ShapeNode>
	{
		private readonly IRule<Word, ShapeNode> _mrulesRule; 
		private readonly IRule<Word, ShapeNode> _prulesRule;
		private readonly IRule<Word, ShapeNode> _templatesRule;
		private readonly Stratum _stratum;
		private readonly Morpher _morpher;

		public AnalysisStratumRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Stratum stratum)
		{
			_stratum = stratum;
			_morpher = morpher;
			_prulesRule = new LinearRuleCascade<Word, ShapeNode>(stratum.PhonologicalRules.Select(prule => prule.CompileAnalysisRule(spanFactory, morpher)).Reverse());
			_templatesRule = new RuleBatch<Word, ShapeNode>(stratum.AffixTemplates.Select(template => template.CompileAnalysisRule(spanFactory, morpher)), false, ValueEqualityComparer<Word>.Default);
			_mrulesRule = null;
			IEnumerable<IRule<Word, ShapeNode>> mrules = stratum.MorphologicalRules.Select(mrule => mrule.CompileAnalysisRule(spanFactory, morpher)).Reverse();
			switch (stratum.MorphologicalRuleOrder)
			{
				case MorphologicalRuleOrder.Linear:
					_mrulesRule = new LinearRuleCascade<Word, ShapeNode>(mrules, true, ValueEqualityComparer<Word>.Default);
					break;
				case MorphologicalRuleOrder.Unordered:
					_mrulesRule = new TemplateCombinationRuleCascade(mrules, _templatesRule);
					break;
			}
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (_morpher.TraceRules.Contains(_stratum))
				input.CurrentTrace.Children.Add(new Trace(TraceType.StratumAnalysisInput, _stratum) {Input = input});

			input = input.DeepClone();
			input.Stratum = _stratum;

			_prulesRule.Apply(input);
			input.Freeze();

			var output = new HashSet<Word>(ValueEqualityComparer<Word>.Default) {input};
			foreach (Word tempWord in _templatesRule.Apply(input).Concat(input))
			{
				output.UnionWith(_mrulesRule.Apply(tempWord));
				output.Add(tempWord);
			}

			if (_morpher.TraceRules.Contains(_stratum))
			{
				foreach (Word outWord in output)
					outWord.CurrentTrace.Children.Add(new Trace(TraceType.StratumAnalysisOutput, _stratum) {Output = outWord});
			}

			return output;
		}

		private class TemplateCombinationRuleCascade : CombinationRuleCascade<Word, ShapeNode>
		{
			private readonly IRule<Word, ShapeNode> _templatesRule; 

			public TemplateCombinationRuleCascade(IEnumerable<IRule<Word, ShapeNode>> rules, IRule<Word, ShapeNode> templatesRule)
				: base(rules, true, ValueEqualityComparer<Word>.Default)
			{
				_templatesRule = templatesRule;
			}

			protected override IEnumerable<Word> ApplyRule(IRule<Word, ShapeNode> rule, int index, Word input)
			{
				foreach (Word outWord in rule.Apply(input))
				{
					foreach (Word tempWord in _templatesRule.Apply(outWord))
						yield return tempWord;

					yield return outWord;
				}
			}
		}
	}
}
