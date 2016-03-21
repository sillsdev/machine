using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab
{
	internal class AnalysisLanguageRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly List<Stratum> _strata;
		private readonly List<IRule<Word, ShapeNode>> _rules;

		public AnalysisLanguageRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Language language)
		{
			_morpher = morpher;
			_strata = language.Strata.Reverse().ToList();
			_rules = _strata.Select(stratum => stratum.CompileAnalysisRule(spanFactory, morpher)).ToList();
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var inputSet = new HashSet<Word>(FreezableEqualityComparer<Word>.Default){input};
			var tempSet = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
			var results = new HashSet<Word>(FreezableEqualityComparer<Word>.Default);
			for (int i = 0; i < _rules.Count && inputSet.Count > 0; i++)
			{
				if (!_morpher.RuleSelector(_strata[i]))
					continue;

				HashSet<Word> outputSet = tempSet;
				outputSet.Clear();

				foreach (Word inData in inputSet)
				{
					foreach (Word outData in _rules[i].Apply(inData))
					{
						outputSet.Add(outData);
						results.Add(outData);
					}
				}

				tempSet = inputSet;
				inputSet = outputSet;
			}

			return results;
		}
	}
}