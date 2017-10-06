using SIL.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.Translation
{
	public static class TranslationExtensions
	{
		public static IReadOnlyList<string> TranslateWord(this ITranslationEngine engine, string sourceWord)
		{
			TranslationResult result = engine.Translate(new[] {sourceWord});
			if (result.TargetWordSources.Any(s => s == TranslationSources.None))
				return new string[0];
			return result.TargetSegment;
		}

		public static IEnumerable<int> GetSuggestedWordIndices(this IInteractiveTranslationSession session,
			double confidenceThreshold)
		{
			return TranslationSuggester.GetSuggestedWordIndices(session.Prefix, session.IsLastWordComplete,
				session.CurrentResult, confidenceThreshold);
		}

		public static void AppendSuggestionToPrefix(this IInteractiveTranslationSession session,
			IReadOnlyList<int> suggestion, int suggestionIndex = -1)
		{
			IEnumerable<int> insert = suggestionIndex == -1 ? suggestion : suggestion[suggestionIndex].ToEnumerable();
			session.AppendToPrefix(insert.Select(j => session.CurrentResult.TargetSegment[j]));
		}

		public static string RecaseTargetWord(this TranslationResult result, IReadOnlyList<string> sourceSegment,
			int targetIndex)
		{
			return result.Alignment.RecaseTargetWord(sourceSegment, 0, result.TargetSegment, targetIndex);
		}

		public static string RecaseTargetWord(this WordAlignmentMatrix alignment, IReadOnlyList<string> sourceSegment,
			int sourceStartIndex, IReadOnlyList<string> targetSegment, int targetIndex)
		{
			string targetWord = targetSegment[targetIndex];
			if (alignment.GetColumnAlignedIndices(targetIndex)
				.Any(i => sourceSegment[sourceStartIndex + i].IsTitleCase()))
			{
				return targetWord.ToTitleCase();
			}
			return targetWord;
		}

		public static bool IsTitleCase(this string str)
		{
			return str.Length > 0 && char.IsUpper(str, 0)
				&& Enumerable.Range(1, str.Length - 1).All(i => char.IsLower(str, i));
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
