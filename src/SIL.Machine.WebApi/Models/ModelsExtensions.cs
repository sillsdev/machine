using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public static class ModelsExtensions
	{
		private static readonly Regex TokenizeRegex = new Regex(@"[\p{P}]|(\w+([.,\-’']\w+)*)");
		private static readonly HashSet<string> MergeRightTokens = new HashSet<string> {"‘", "“", "(", "¿", "¡", "«"};
		private static readonly HashSet<string> MergeRightFirstLeftSecondTokens = new HashSet<string> {"\"", "'"};

		public static IEnumerable<string> Tokenize(this string str)
		{
			return TokenizeRegex.Matches(str).Cast<Match>().Select(m => m.Value.ToLowerInvariant());
		}

		public static IEnumerable<RangeDto> TokenizeRanges(this string str)
		{
			return TokenizeRegex.Matches(str).Cast<Match>().Select(m => new RangeDto(m.Index, m.Index + m.Length));
		}

		public static string Detokenize(this IEnumerable<string> tokens)
		{
			var currentRightLeftTokens = new HashSet<string>();
			var sb = new StringBuilder();
			bool nextMergeLeft = true;
			foreach (string token in tokens)
			{
				bool mergeRight = false;
				if (token.All(char.IsPunctuation))
				{
					if (MergeRightTokens.Contains(token))
					{
						mergeRight = true;
					}
					else if (MergeRightFirstLeftSecondTokens.Contains(token))
					{
						if (currentRightLeftTokens.Contains(token))
						{
							nextMergeLeft = true;
							currentRightLeftTokens.Remove(token);
						}
						else
						{
							mergeRight = true;
							currentRightLeftTokens.Add(token);
						}
					}
					else
					{
						nextMergeLeft = true;
					}
				}

				if (!nextMergeLeft)
					sb.Append(" ");
				else
					nextMergeLeft = false;

				sb.Append(token);

				if (mergeRight)
					nextMergeLeft = true;
			}
			return sb.ToString();
		}

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
			string targetSegment = suggestion.SuggestedWordIndices.Select(suggestion.TranslationResult.RecaseTargetWord).Detokenize();
			return new SuggestionDto(targetSegment, GetTargetWords(suggestion.TranslationResult, suggestion.SourceSegment, suggestion.Prefix));
		}

		private static IEnumerable<TargetWordDto> GetTargetWords(TranslationResult result, string sourceSegment, string targetSegment)
		{
			RangeDto[] sourceSegmentRanges = sourceSegment.TokenizeRanges().ToArray();
			RangeDto[] targetSegmentRanges = targetSegment.TokenizeRanges().ToArray();
			for (int i = 0; i < targetSegmentRanges.Length; i++)
				yield return new TargetWordDto(targetSegmentRanges[i], result.GetTargetWordPairs(i).Select(wp => new SourceWordDto(sourceSegmentRanges[wp.SourceIndex], wp.Confidence)));
		}
	}
}
