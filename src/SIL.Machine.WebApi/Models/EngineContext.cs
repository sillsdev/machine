using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public class EngineContext
	{
		private static readonly HashSet<string> MergeRightTokens = new HashSet<string> {"‘", "“", "(", "¿", "¡", "«"};
		private static readonly HashSet<string> MergeRightFirstLeftSecondTokens = new HashSet<string> {"\"", "'"};

		public EngineContext(string sourceLanguageTag, string targetLanguageTag)
		{
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			Tokenizer = new RegexTokenizer(new IntegerSpanFactory(), @"[\p{P}]|(\w+([.,\-’']\w+)*)");
			Detokenizer = new SimpleStringDetokenizer(GetDetokenizeOperation);
		}

		private static DetokenizeOperation GetDetokenizeOperation(string token)
		{
			if (token.Any(char.IsPunctuation))
			{
				if (MergeRightTokens.Contains(token))
					return DetokenizeOperation.MergeRight;
				if (MergeRightFirstLeftSecondTokens.Contains(token))
					return DetokenizeOperation.MergeRightFirstLeftSecond;
				return DetokenizeOperation.MergeLeft;
			}

			return DetokenizeOperation.NoOperation;
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public HybridTranslationEngine Engine { get; set; }
		public ITokenizer<string, int> Tokenizer { get; }
		public IDetokenizer<string, string> Detokenizer { get; }
		public int SessionCount { get; set; }
	}
}
