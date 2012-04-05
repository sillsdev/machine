using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	public class LanguageSynthesisRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly List<Tuple<Stratum, IRule<Word, ShapeNode>>> _strataRules;

		public LanguageSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Language lang)
		{
			_morpher = morpher;
			_strataRules = lang.Strata.Select(str => Tuple.Create(str, str.CompileSynthesisRule(spanFactory, morpher))).ToList();
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var inWords = new HashSet<Word>();
			var outWords = new HashSet<Word>();
			for (int i = 0; i < _strataRules.Count; i++)
			{
				// start applying at the stratum that this lex entry belongs to
				if (_strataRules[i].Item1 == input.RootAllomorph.Morpheme.Stratum)
					inWords.Add(input);

				outWords.Clear();
				foreach (Word inWord in inWords)
				{
					if (_morpher.TraceRules.Contains(_strataRules[i].Item1))
						inWord.CurrentTrace.Children.Add(new Trace(TraceType.StratumSynthesisInput, _strataRules[i].Item1) { Input = inWord.DeepClone() });

					foreach (Word outWord in _strataRules[i].Item2.Apply(inWord).Concat(inWord))
					{
						// promote the word synthesis to the next stratum
						if (i != _strataRules.Count - 1)
							outWord.Stratum = _strataRules[i + 1].Item1;

						if (_morpher.TraceRules.Contains(_strataRules[i].Item1))
							outWord.CurrentTrace.Children.Add(new Trace(TraceType.StratumSynthesisOutput, _strataRules[i].Item1) { Output = outWord.DeepClone() });

						outWords.Add(outWord);
					}
				}

				inWords.Clear();
				inWords.UnionWith(outWords);
			}

			return outWords;
		}
	}
}
