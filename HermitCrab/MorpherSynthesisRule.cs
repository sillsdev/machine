using System.Collections.Generic;
using SIL.APRE.Transduction;
using SIL.APRE;

namespace SIL.HermitCrab
{
	public class MorpherSynthesisRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;

		public MorpherSynthesisRule(Morpher morpher)
		{
			_morpher = morpher;
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public bool Apply(Word input, out IEnumerable<Word> output)
		{
			var inWords = new HashSet<Word>();
			var outWords = new HashSet<Word>();
			var strata = new List<Stratum>(_morpher.Strata);
			for (int i = 0; i < strata.Count; i++)
			{
				// start applying at the stratum that this lex entry belongs to
				if (strata[i] == input.Root.Stratum)
					inWords.Add(input);

				outWords.Clear();
				foreach (Word inWord in inWords)
				{
					IEnumerable<Word> stratumOutput = strata[i].SynthesisRule.Apply(inWord, out stratumOutput) ? stratumOutput.Concat(inWord) : inWord.ToEnumerable();
					foreach (Word outWord in stratumOutput)
					{
						// promote the word synthesis to the next stratum
						if (i != strata.Count - 1)
							outWord.Stratum = strata[i + 1];

						outWords.Add(outWord);
					}
				}

				inWords.Clear();
				inWords.UnionWith(outWords);
			}

			output = outWords;
			return true;
		}
	}
}
