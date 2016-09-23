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
			for (int i = 0; i < result.SourceSegment.Count; i++)
			{
				int wordPairCount = result.GetSourceWordPairs(i).Count();
				if (wordPairCount == 0)
					lookaheadCount++;
				else
					lookaheadCount += wordPairCount - 1;
			}
			int j;
			for (j = 0; j < result.TargetSegment.Count; j++)
			{
				int wordPairCount = result.GetTargetWordPairs(j).Count();
				if (wordPairCount == 0)
					lookaheadCount++;
			}
			j = session.Prefix.Count;
			// ensure that we include a partial word as a suggestion
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
