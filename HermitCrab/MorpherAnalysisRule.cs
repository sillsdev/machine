using System.Linq;
using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Transduction;

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

		public IEnumerable<Word> Apply(Word input)
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
					foreach (Word outWord in strata[i].AnalysisRule.Apply(inWord).Concat(inWord))
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

			return outputList;
		}
	}
}
