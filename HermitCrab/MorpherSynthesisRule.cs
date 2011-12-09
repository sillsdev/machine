using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Transduction;

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

		public IEnumerable<Word> Apply(Word input)
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
					foreach (Word outWord in strata[i].SynthesisRule.Apply(inWord).Concat(inWord))
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

			return outWords;
		}
	}
}
