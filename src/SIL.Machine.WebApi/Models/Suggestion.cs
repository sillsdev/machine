using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Translation;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class Suggestion
	{
		public Suggestion(IEnumerable<string> suggestedWords, IEnumerable<Span<int>> sourceSegmentTokens, IEnumerable<Span<int>> prefixTokens,
			TranslationResult result)
		{
			Words = new ReadOnlyList<string>(suggestedWords.ToArray());
			SourceSegmentTokens = new ReadOnlyList<Span<int>>(sourceSegmentTokens.ToArray());
			PrefixTokens = new ReadOnlyList<Span<int>>(prefixTokens.ToArray());
			TranslationResult = result;
		}

		public ReadOnlyList<string> Words { get; }
		public ReadOnlyList<Span<int>> SourceSegmentTokens { get; }
		public ReadOnlyList<Span<int>> PrefixTokens { get; }
		public TranslationResult TranslationResult { get; }
	}
}
