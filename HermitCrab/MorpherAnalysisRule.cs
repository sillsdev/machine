using System.Linq;
using SIL.APRE;
using System.Collections.Generic;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class MorpherAnalysisRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;

		public MorpherAnalysisRule(Morpher morpher)
		{
			_morpher = morpher;
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public bool Apply(Word input, out IEnumerable<Word> output)
		{
			var inWords = new List<Word> {input};
			var outWords = new List<Word>();
			var outputList = new List<Word>();
			var strata = new List<Stratum>(_morpher.Strata.Reverse());
			for (int i = 0; i < strata.Count; i++)
			{
				outWords.Clear();
				foreach (Word inWord in inWords)
				{
					IEnumerable<Word> stratumOutput;
					stratumOutput = strata[i].AnalysisRule.Apply(inWord, out stratumOutput) ? stratumOutput.Concat(inWord) : inWord.ToEnumerable();

					foreach (Word outWord in stratumOutput)
					{
						outputList.Add(outWord);

						Word newWord = outWord.Clone();
						// promote each analysis to the next stratum
						if (i != strata.Count - 1)
							newWord.Stratum = strata[i + 1];
						outWords.Add(newWord);
					}
				}

				inWords.Clear();
				inWords.AddRange(outWords);
			}

			output = outputList;
			return true;
		}
	}
}
