using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Translation
{
	public static class TranslationExtensions
	{
		public static TranslationResult AddToPrefix(this IInteractiveTranslationSession session, string addition, bool isLastWordComplete)
		{
			return session.AddToPrefix(addition.ToEnumerable(), isLastWordComplete);
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
			return WordSuggester.GetSuggestedWordIndices(session.Prefix, session.IsLastWordComplete, session.CurrenTranslationResult, confidenceThreshold);
		}
	}
}
