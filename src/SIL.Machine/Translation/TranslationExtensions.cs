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
	}
}
