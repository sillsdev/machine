using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	internal static class ModelsExtensions
	{
		public static string RecaseTargetWord(this TranslationResult result, IList<string> sourceSegment, int targetIndex)
		{
			string token = result.TargetSegment[targetIndex];
			if (result.GetTargetWordPairs(targetIndex).Any(awi => IsCapitalCase(sourceSegment[awi.SourceIndex])))
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
			return new SessionDto
			{
				Id = sessionContext.Id,
				SourceSegment = sessionContext.SourceSegment,
				Prefix = sessionContext.Prefix,
				ConfidenceThreshold = sessionContext.ConfidenceThreshold
			};
		}
	}
}
