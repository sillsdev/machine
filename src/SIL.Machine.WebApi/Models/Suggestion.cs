using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Translation;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class Suggestion
	{
		public Suggestion(TranslationResult result, IEnumerable<int> suggestedWordIndices, string sourceSegment, string prefix)
		{
			TranslationResult = result;
			SuggestedWordIndices = new ReadOnlyList<int>(suggestedWordIndices.ToArray());
			SourceSegment = sourceSegment;
			Prefix = prefix;
		}

		public TranslationResult TranslationResult { get; }
		public ReadOnlyList<int> SuggestedWordIndices { get; }
		public string SourceSegment { get; }
		public string Prefix { get; }
	}
}
