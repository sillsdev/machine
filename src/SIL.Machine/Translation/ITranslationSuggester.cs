using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public interface ITranslationSuggester
    {
        double ConfidenceThreshold { get; set; }
        bool BreakOnPunctuation { get; set; }

        IReadOnlyList<TranslationSuggestion> GetSuggestions(
            int n,
            int prefixCount,
            bool isLastWordComplete,
            IEnumerable<TranslationResult> results
        );
    }
}
