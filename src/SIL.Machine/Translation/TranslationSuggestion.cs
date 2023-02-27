using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class TranslationSuggestion
    {
        public TranslationSuggestion(TranslationResult result)
            : this(result, Enumerable.Empty<int>(), 0) { }

        public TranslationSuggestion(TranslationResult result, IEnumerable<int> indices, double confidence)
        {
            Result = result;
            TargetWordIndices = indices.ToArray();
            Confidence = confidence;
        }

        public TranslationResult Result { get; }
        public IReadOnlyList<int> TargetWordIndices { get; }
        public double Confidence { get; }

        public IEnumerable<string> TargetWords => TargetWordIndices.Select(i => Result.TargetSegment[i]);
    }
}
