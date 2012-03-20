using System;
using System.Linq;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Rules;

namespace SIL.HermitCrab
{
	public class LanguageAnalysisRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly List<Tuple<Stratum, IRule<Word, ShapeNode>>> _strataRules; 

		public LanguageAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, Language lang)
		{
			_morpher = morpher;
			_strataRules = lang.Strata.Reverse().Select(str => Tuple.Create(str, str.CompileAnalysisRule(spanFactory, morpher))).ToList();
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public IEnumerable<Word> Apply(Word input)
		{
			var inWords = new List<Word> {input};
			var outWords = new List<Word>();
			var outputList = new List<Word>();
			for (int i = 0; i < _strataRules.Count; i++)
			{
				outWords.Clear();
				foreach (Word inWord in inWords)
				{
					if (_morpher.GetTraceRule(_strataRules[i].Item1))
						inWord.CurrentTrace.Children.Add(new Trace(TraceType.StratumAnalysisInput, _strataRules[i].Item1) { Input = inWord.DeepClone() });

					foreach (Word outWord in _strataRules[i].Item2.Apply(inWord).Concat(inWord))
					{
						outputList.Add(outWord);

						Word newWord = outWord.DeepClone();
						// promote each analysis to the next stratum
						if (i != _strataRules.Count - 1)
							newWord.Stratum = _strataRules[i + 1].Item1;

						if (_morpher.GetTraceRule(_strataRules[i].Item1))
							outWord.CurrentTrace.Children.Add(new Trace(TraceType.StratumAnalysisOutput, _strataRules[i].Item1) { Output = outWord.DeepClone() });
						outWords.Add(newWord);
					}
				}

				inWords.Clear();
				inWords.AddRange(outWords);
			}

			return outputList;
		}
	}
}
