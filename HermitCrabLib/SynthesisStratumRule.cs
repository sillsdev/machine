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
			_mrulesRule = null;
			IEnumerable<IRule<Word, ShapeNode>> mrules = stratum.MorphologicalRules.Select(mrule => mrule.CompileSynthesisRule(spanFactory, morpher)).Concat(templatesRule);
			switch (stratum.MorphologicalRuleOrder)
			{
				case MorphologicalRuleOrder.Linear:
					_mrulesRule = new LinearRuleCascade<Word, ShapeNode>(mrules, true, FreezableEqualityComparer<Word>.Default);
					break;
				case MorphologicalRuleOrder.Unordered:
					_mrulesRule = new CombinationRuleCascade<Word, ShapeNode>(mrules, true, FreezableEqualityComparer<Word>.Default);
					break;
			}
			_prulesRule = new LinearRuleCascade<Word, ShapeNode>(stratum.PhonologicalRules.Select(prule => prule.CompileSynthesisRule(spanFactory, morpher)));
			_stratum = stratum;
			_morpher = morpher;
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (input.RootAllomorph.Morpheme.Stratum.Depth > _stratum.Depth)
				return input.ToEnumerable();

			if (_morpher.TraceRules.Contains(_stratum))
				input.CurrentTrace.Children.Add(new Trace(TraceType.StratumSynthesisInput, _stratum) {Input = input});
			var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
			foreach (Word mruleOutWord in _mrulesRule.Apply(input))
			{
				Word newWord = mruleOutWord.DeepClone();
				_prulesRule.Apply(newWord);
				newWord.Freeze();
				if (_morpher.TraceRules.Contains(_stratum))
					newWord.CurrentTrace.Children.Add(new Trace(TraceType.StratumSynthesisOutput, _stratum) {Output = newWord});
				output.Add(newWord);
			}
			if (output.Count == 0 && _morpher.TraceRules.Contains(_stratum))
				input.CurrentTrace.Children.Add(new Trace(TraceType.StratumSynthesisOutput, _stratum));
			return output;
		}
	}
}
