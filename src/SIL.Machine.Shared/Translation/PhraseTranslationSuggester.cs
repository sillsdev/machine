using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class PhraseTranslationSuggester : ITranslationSuggester
	{
		public PhraseTranslationSuggester(double confidenceThreshold)
		{
			ConfidenceThreshold = confidenceThreshold;
		}

		public double ConfidenceThreshold { get; set; }

		public IEnumerable<int> GetSuggestedWordIndices(int prefixCount, bool isLastWordComplete,
			TranslationResult result)
		{
			int startingJ = prefixCount;
			if (!isLastWordComplete)
			{
				// if the prefix ends with a partial word and it has been completed,
				// then make sure it is included as a suggestion,
				// otherwise, don't return any suggestions
				if ((result.WordSources[startingJ - 1] & TranslationSources.Smt) != 0)
					startingJ--;
				else
					yield break;
			}

			int k = 0;
			while (k < result.Phrases.Count && result.Phrases[k].TargetSegmentCut <= startingJ)
				k++;

			for (; k < result.Phrases.Count; k++)
			{
				Phrase phrase = result.Phrases[k];
				if (phrase.Confidence >= ConfidenceThreshold)
				{
					for (int j = startingJ; j < phrase.TargetSegmentCut; j++)
					{
						string word = result.TargetSegment[j];
						TranslationSources sources = result.WordSources[j];
						if (sources == TranslationSources.None || word.All(char.IsPunctuation))
							yield break;
						yield return j;
					}
					startingJ = phrase.TargetSegmentCut;
				}
				else
				{
					yield break;
				}
			}
		}
	}
}
