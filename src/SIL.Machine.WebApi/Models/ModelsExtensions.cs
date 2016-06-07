using System.Linq;
using System.Text;
using SIL.Machine.Annotations;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public static class ModelsExtensions
	{
		public static string RecaseTargetWord(this TranslationResult result, int targetIndex)
		{
			string token = result.TargetSegment[targetIndex];
			if (result.GetTargetWordPairs(targetIndex).Any(awi => IsCapitalCase(result.SourceSegment[awi.SourceIndex])))
				token = ToCapitalCase(token);
			return token;
		}

		private static bool IsCapitalCase(string token)
		{
			return token.Length > 0 && char.IsUpper(token, 0) && Enumerable.Range(1, token.Length - 1).All(i => char.IsLower(token, i));
		}

		private static string ToCapitalCase(string word)
		{
			if (word.Length == 0)
				return word;

			var sb = new StringBuilder();
			sb.Append(word.Substring(0, 1).ToUpperInvariant());
			if (word.Length > 1)
				sb.Append(word.Substring(1, word.Length - 1).ToLowerInvariant());
			return sb.ToString();
		}

		public static EngineDto CreateDto(this EngineContext engineContext)
		{
			return new EngineDto
			{
				SourceLanguageTag = engineContext.SourceLanguageTag,
				TargetLanguageTag = engineContext.TargetLanguageTag
			};
		}

		public static SessionDto CreateDto(this SessionContext sessionContext)
		{
			using (sessionContext.EngineContext.Mutex.Lock())
			{
				return new SessionDto
				{
					Id = sessionContext.Id,
					SourceSegment = sessionContext.SourceSegment,
					Prefix = sessionContext.Prefix,
					ConfidenceThreshold = sessionContext.ConfidenceThreshold
				};
			}
		}

		public static SuggestionDto CreateDto(this Suggestion suggestion)
		{
			return new SuggestionDto
			{
				Suggestion = suggestion.Words.ToArray(),
				Alignment = suggestion.PrefixTokens.Select((prefixSpan, j) => new TargetWordDto
				{
					Range = new[] {prefixSpan.Start, prefixSpan.End},
					SourceWords = suggestion.TranslationResult.GetTargetWordPairs(j).Select(wp =>
					{
						Span<int> sourceSpan = suggestion.SourceSegmentTokens[wp.SourceIndex];
						return new SourceWordDto
						{
							Range = new[] {sourceSpan.Start, sourceSpan.End},
							Confidence = wp.Confidence
						};
					}).ToArray()
				}).ToArray()
			};
		}
	}
}
