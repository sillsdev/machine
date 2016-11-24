using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Translation
{
	public static class TranslationExtensions
	{
		public static TranslationResult AddToPrefix(this IInteractiveTranslationSession session, string addition, bool isLastWordPartial)
		{
			return session.AddToPrefix(addition.ToEnumerable(), isLastWordPartial);
		}

		public static IEnumerable<string> TranslateWord(this ITranslationEngine engine, string sourceWord)
		{
			TranslationResult result = engine.Translate(sourceWord.ToEnumerable());
			if (result.GetSourceWordPairs(0).Any(wp => wp.Sources == TranslationSources.None))
				return Enumerable.Empty<string>();
			return result.TargetSegment;
		}

		public static IEnumerable<int> GetSuggestedWordIndices(this IInteractiveTranslationSession session, double confidenceThreshold)
		{
			TranslationResult result = session.CurrenTranslationResult;
			int lookaheadCount = 1;
			int i = -1, j;
			for (j = session.Prefix.Count; j < result.TargetSegment.Count; j++)
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
			j = session.Prefix.Count;
			// ensure that we include a partial word as a suggestion
			// TODO: only include the last word if it has been completed by the SMT
			if (session.IsLastWordPartial)
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
					&& (inPhrase || !session.IsLastWordPartial || result.TargetSegment[j].StartsWith(session.Prefix[session.Prefix.Count - 1])))
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
