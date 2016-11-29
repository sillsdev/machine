using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public static class WordSuggester
	{
		public static IEnumerable<int> GetSuggestedWordIndices(IReadOnlyList<string> prefix, bool isLastWordComplete, TranslationResult result, double confidenceThreshold)
		{
			int lookaheadCount = 1;
			int i = -1, j;
			for (j = prefix.Count; j < result.TargetSegment.Count; j++)
			{
				AlignedWordPair[] wordPairs = result.GetTargetWordPairs(j).ToArray();
				if (wordPairs.Length == 0)
				{
					lookaheadCount++;
				}
				else
				{
					lookaheadCount += wordPairs.Length - 1;
					foreach (AlignedWordPair wordPair in wordPairs)
					{
						if (i == -1 || wordPair.SourceIndex < i)
							i = wordPair.SourceIndex;
					}
				}
			}
			if (i == -1)
				i = 0;
			for (; i < result.SourceSegment.Count; i++)
			{
				if (!result.GetSourceWordPairs(i).Any())
					lookaheadCount++;
			}
			j = prefix.Count;
			// ensure that we include a partial word as a suggestion
			// TODO: only include the last word of prefix if it has not been corrected
			if (!isLastWordComplete)
				j--;
			bool inPhrase = false;
			while (j < result.TargetSegment.Count && (lookaheadCount > 0 || inPhrase))
			{
				string word = result.TargetSegment[j];
				// stop suggesting at punctuation
				if (word.All(char.IsPunctuation))
					break;

				if ((result.GetTargetWordConfidence(j) >= confidenceThreshold
					|| result.GetTargetWordPairs(j).Any(awi => (awi.Sources & TranslationSources.Transfer) == TranslationSources.Transfer))
					&& (inPhrase || isLastWordComplete || result.TargetSegment[j].StartsWith(prefix[prefix.Count - 1])))
				{
					yield return j;
					inPhrase = true;
					lookaheadCount--;
				}
				else
				{
					// skip over inserted words
					if (result.GetTargetWordPairs(j).Any())
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
