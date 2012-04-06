using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	internal class SynthesisStratumRule : IRule<Word, ShapeNode>
	{
		private readonly IRule<Word, ShapeNode> _mrulesRule;
		private readonly IRule<Word, ShapeNode> _prulesRule;
		private readonly Stratum _stratum;
		private readonly Morpher _morpher;

		public SynthesisStratumRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Stratum stratum)
		{
			var templatesRule = new SynthesisAffixTemplatesRule(spanFactory, morpher, stratum);
			_mrulesRule = new RuleCascade<Word, ShapeNode>(stratum.MorphologicalRules.Select(mrule => mrule.CompileSynthesisRule(spanFactory, morpher)).Concat(templatesRule),
				stratum.MorphologicalRuleOrder, true, FreezableEqualityComparer<Word>.Instance);
			_prulesRule = new RuleCascade<Word, ShapeNode>(stratum.PhonologicalRules.Select(prule => prule.CompileSynthesisRule(spanFactory, morpher)), stratum.PhonologicalRuleOrder);
			_stratum = stratum;
			_morpher = morpher;
		}

		public bool IsApplicable(Word input)
		{
			return input.RootAllomorph.Morpheme.Stratum.Depth <= _stratum.Depth;
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (_morpher.TraceRules.Contains(_stratum))
				input.CurrentTrace.Children.Add(new Trace(TraceType.StratumSynthesisInput, _stratum) {Input = input});
			var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Instance);
			foreach (Word mruleOutWord in _mrulesRule.Apply(input))
			{
				Word newWord = mruleOutWord.DeepClone();
				_prulesRule.Apply(newWord);
				newWord.Freeze();
				if (_morpher.TraceRules.Contains(_stratum))
					newWord.CurrentTrace.Children.Add(new Trace(TraceType.StratumSynthesisOutput, _stratum) {Output = newWord});
				output.Add(newWord);
			}
			return output;
		}
	}
}
