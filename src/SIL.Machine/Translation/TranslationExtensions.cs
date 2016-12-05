using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public static bool IsTitleCase(this string str)
		{
			return str.Length > 0 && char.IsUpper(str, 0) && Enumerable.Range(1, str.Length - 1).All(i => char.IsLower(str, i));
		}

		public static string ToTitleCase(this string str)
		{
			if (str.Length == 0)
				return str;

			var sb = new StringBuilder();
			sb.Append(str.Substring(0, 1).ToUpperInvariant());
			if (str.Length > 1)
				sb.Append(str.Substring(1, str.Length - 1).ToLowerInvariant());
			return sb.ToString();
		}
	}
}
