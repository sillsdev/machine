using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	internal class SynthesisStratumRule : IRule<Word, ShapeNode>
	{
		private readonly IRule<Word, ShapeNode> _mrulesRule;
		private readonly IRule<Word, ShapeNode> _prulesRule;
		private readonly SynthesisAffixTemplatesRule _templatesRule; 
		private readonly Stratum _stratum;
		private readonly Morpher _morpher;

		public SynthesisStratumRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Stratum stratum)
		{
			_templatesRule = new SynthesisAffixTemplatesRule(spanFactory, morpher, stratum);
			_mrulesRule = null;
			IEnumerable<IRule<Word, ShapeNode>> mrules = stratum.MorphologicalRules.Select(mrule => mrule.CompileSynthesisRule(spanFactory, morpher));
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
			if (!_morpher.RuleSelector(_stratum) || input.RootAllomorph.Morpheme.Stratum.Depth > _stratum.Depth)
				return input.ToEnumerable();

			_morpher.TraceManager.BeginApplyStratum(_stratum, input);

			IEnumerable<Word> mruleOutWords = null;
			switch (_stratum.MorphologicalRuleOrder)
			{
				case MorphologicalRuleOrder.Linear:
					mruleOutWords = ApplyMorphologicalRules(input);
					break;

				case MorphologicalRuleOrder.Unordered:
					mruleOutWords = ApplyMorphologicalRules(input).Concat(ApplyTemplates(input));
					break;
			}
			Debug.Assert(mruleOutWords != null);

			var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
			foreach (Word mruleOutWord in mruleOutWords)
			{
				if (mruleOutWord.IsLastAppliedRuleFinal)
				{
					Word newWord = mruleOutWord.DeepClone();
					_prulesRule.Apply(newWord);
					newWord.Freeze();
					_morpher.TraceManager.EndApplyStratum(_stratum, newWord);
					output.Add(newWord);
				}
			}
			if (output.Count == 0)
				_morpher.TraceManager.EndApplyStratum(_stratum, input);
			return output;
		}

		private IEnumerable<Word> ApplyMorphologicalRules(Word input)
		{
			foreach (Word mruleOutWord in _mrulesRule.Apply(input))
			{
				Word word = mruleOutWord;
				if (!word.IsLastAppliedRuleFinal)
				{
					word = word.DeepClone();
					word.IsLastAppliedRuleFinal = true;
					word.Freeze();
				}

				foreach (Word tempOutWord in ApplyTemplates(word))
					yield return tempOutWord;
				yield return word;
			}
		}

		private IEnumerable<Word> ApplyTemplates(Word input)
		{
			foreach (Word tempOutWord in _templatesRule.Apply(input))
			{
				switch (_stratum.MorphologicalRuleOrder)
				{
					case MorphologicalRuleOrder.Linear:
						yield return tempOutWord;
						break;

					case MorphologicalRuleOrder.Unordered:
						if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
						{
							foreach (Word outWord in ApplyMorphologicalRules(tempOutWord))
								yield return outWord;
						}
						yield return tempOutWord;
						break;
				}
			}
		}
	}
}
