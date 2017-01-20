using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public static class TranslationSuggester
	{
		public static IEnumerable<int> GetSuggestedWordIndices(IReadOnlyList<string> prefix, bool isLastWordComplete, TranslationResult result, double confidenceThreshold)
		{
			int startingJ = prefix.Count;
			if (!isLastWordComplete)
			{
				// if the prefix ends with a partial word and it has been completed,
				// then make sure it is included as a suggestion,
				// otherwise, don't return any suggestions
				if ((result.TargetWordSources[startingJ - 1] & TranslationSources.Smt) != 0)
					startingJ--;
				else
					yield break;
			}

			int lookaheadCount = 1;
			int i = -1, j;
			for (j = prefix.Count; j < result.TargetSegment.Count; j++)
			{
				int[] sourceIndices = result.Alignment.GetColumnWordAlignedIndices(j).ToArray();
				if (sourceIndices.Length == 0)
				{
					lookaheadCount++;
				}
				else
				{
					lookaheadCount += sourceIndices.Length - 1;
					foreach (int ti in sourceIndices)
					{
						if (i == -1 || ti < i)
							i = ti;
					}
				}
			}
			if (i == -1)
				i = 0;
			for (; i < result.SourceSegment.Count; i++)
			{
				if (result.Alignment.IsRowWordAligned(i) == AlignmentType.NotAligned)
					lookaheadCount++;
			}

			j = startingJ;
			bool inPhrase = false;
			while (j < result.TargetSegment.Count && (lookaheadCount > 0 || inPhrase))
			{
				string word = result.TargetSegment[j];
				// stop suggesting at punctuation
				if (word.All(char.IsPunctuation))
					break;

				// criteria for suggesting a word
				// the word must either:
				// - meet the confidence threshold
				// - come from a transfer engine
				double confidence = result.TargetWordConfidences[j];
				TranslationSources sources = result.TargetWordSources[j];
				if (confidence >= confidenceThreshold || (sources & TranslationSources.Transfer) != 0)
				{
					yield return j;
					inPhrase = true;
					lookaheadCount--;
				}
				else
				{
					// skip over inserted words
					if (result.Alignment.IsColumnWordAligned(j) == AlignmentType.Aligned)
					{
						lookaheadCount--;
						// only suggest the first word/phrase we find
						if (inPhrase)
							break;
					}
				}
				j++;
			}
		}
	}
}
