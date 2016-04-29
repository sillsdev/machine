using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.Translation
{
	public static class TranslationExtensions
	{
		public static TranslationResult AddToPrefix(this IInteractiveTranslator translator, string addition, bool isLastWordPartial)
		{
			return translator.AddToPrefix(addition.ToEnumerable(), isLastWordPartial);
		}

		public static IEnumerable<string> TranslateWord(this ITranslator translator, string sourceWord)
		{
			TranslationResult result = translator.Translate(sourceWord.ToEnumerable());
			if (result.GetSourceWordPairs(0).Any(wp => wp.Sources == TranslationSources.None))
				return Enumerable.Empty<string>();
			return result.TargetSegment;
		}
	}
}
