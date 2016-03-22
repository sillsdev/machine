using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.HermitCrab
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
			_templatesRule = new RuleBatch<Word, ShapeNode>(stratum.AffixTemplates.Select(template => template.CompileAnalysisRule(spanFactory, morpher)), false, FreezableEqualityComparer<Word>.Default);
			_mrulesRule = null;
			IEnumerable<IRule<Word, ShapeNode>> mrules = stratum.MorphologicalRules.Select(mrule => mrule.CompileAnalysisRule(spanFactory, morpher)).Reverse();
			switch (stratum.MorphologicalRuleOrder)
			{
				case MorphologicalRuleOrder.Linear:
					_mrulesRule = new LinearRuleCascade<Word, ShapeNode>(mrules, true, FreezableEqualityComparer<Word>.Default);
					break;
				case MorphologicalRuleOrder.Unordered:
					_mrulesRule = new ParallelCombinationRuleCascade<Word, ShapeNode>(mrules, true, FreezableEqualityComparer<Word>.Default);
					break;
			}
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (_morpher.TraceManager.IsTracing)
				_morpher.TraceManager.BeginUnapplyStratum(_stratum, input);

			input = input.Clone();
			input.Stratum = _stratum;

			_prulesRule.Apply(input);
			input.Freeze();

			IEnumerable<Word> mruleOutWords = null;
			switch (_stratum.MorphologicalRuleOrder)
			{
				case MorphologicalRuleOrder.Linear:
					mruleOutWords = ApplyTemplates(input);
					break;

				case MorphologicalRuleOrder.Unordered:
					mruleOutWords = ApplyTemplates(input).Concat(ApplyMorphologicalRules(input));
					break;
			}
			Debug.Assert(mruleOutWords != null);

			var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default) {input};
			if (_morpher.TraceManager.IsTracing)
				_morpher.TraceManager.EndUnapplyStratum(_stratum, input);
			foreach (Word mruleOutWord in mruleOutWords)
			{
				output.Add(mruleOutWord);
				if (_morpher.TraceManager.IsTracing)
					_morpher.TraceManager.EndUnapplyStratum(_stratum, mruleOutWord);
			}
			return output;
		}

		private IEnumerable<Word> ApplyMorphologicalRules(Word input)
		{
			foreach (Word mruleOutWord in _mrulesRule.Apply(input))
			{
				switch (_stratum.MorphologicalRuleOrder)
				{
					case MorphologicalRuleOrder.Linear:
						yield return mruleOutWord;
						break;

					case MorphologicalRuleOrder.Unordered:
						foreach (Word tempOutWord in ApplyTemplates(mruleOutWord))
							yield return tempOutWord;
						yield return mruleOutWord;
						break;
				}
			}
		}

		private IEnumerable<Word> ApplyTemplates(Word input)
		{
			foreach (Word tempOutWord in _templatesRule.Apply(input))
			{
				switch (_stratum.MorphologicalRuleOrder)
				{
					case MorphologicalRuleOrder.Linear:
						foreach (Word outWord in ApplyMorphologicalRules(tempOutWord))
							yield return outWord;
						if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
							yield return tempOutWord;
						break;

					case MorphologicalRuleOrder.Unordered:
						if (!FreezableEqualityComparer<Word>.Default.Equals(input, tempOutWord))
						{
							foreach (Word outWord in ApplyMorphologicalRules(tempOutWord))
								yield return outWord;
							yield return tempOutWord;
						}
						break;
				}
			}
		}
	}
}
