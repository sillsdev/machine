using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ITranslationSuggester
	{
		double ConfidenceThreshold { get; set; }

		IEnumerable<int> GetSuggestedWordIndices(int prefixCount, bool isLastWordComplete, TranslationResult result);
	}
}
