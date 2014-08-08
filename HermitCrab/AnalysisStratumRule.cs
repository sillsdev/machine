using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	internal class AnalysisStratumRule : IRule<Word, ShapeNode>
	{
		private readonly IRule<Word, ShapeNode> _mrulesRule; 
		private readonly IRule<Word, ShapeNode> _prulesRule;
		private readonly Stratum _stratum;
		private readonly Morpher _morpher;

		public AnalysisStratumRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Stratum stratum)
		{
			_stratum = stratum;
			_morpher = morpher;
			_prulesRule = new LinearRuleCascade<Word, ShapeNode>(stratum.PhonologicalRules.Select(prule => prule.CompileAnalysisRule(spanFactory, morpher)).Reverse());
			IEnumerable<IRule<Word, ShapeNode>> mrules = stratum.MorphologicalRules.Select(mrule => mrule.CompileAnalysisRule(spanFactory, morpher))
				.Concat(stratum.AffixTemplates.Select(template => template.CompileAnalysisRule(spanFactory, morpher)));
			switch (stratum.MorphologicalRuleOrder)
			{
				case MorphologicalRuleOrder.Linear:
					_mrulesRule = new LinearRuleCascade<Word, ShapeNode>(mrules.Reverse(), true, FreezableEqualityComparer<Word>.Default);
					break;
				case MorphologicalRuleOrder.Unordered:
					_mrulesRule = new ParallelCombinationRuleCascade<Word, ShapeNode>(mrules, true, FreezableEqualityComparer<Word>.Default);
					break;
			}
		}

		public IEnumerable<Word> Apply(Word input)
		{
			_morpher.TraceManager.BeginUnapplyStratum(_stratum, input);

			input = input.DeepClone();
			input.Stratum = _stratum;

			_prulesRule.Apply(input);
			input.Freeze();

			var output = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
			foreach (Word outWord in _mrulesRule.Apply(input).Concat(input))
			{
				_morpher.TraceManager.EndUnapplyStratum(_stratum, outWord);
				output.Add(outWord);
			}

			return output;
		}
	}
}
