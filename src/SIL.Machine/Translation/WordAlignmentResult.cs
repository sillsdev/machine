using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public class WordAlignmentResult
    {
        public WordAlignmentResult(
            IEnumerable<string> sourceTokens,
            IEnumerable<string> targetTokens,
            IEnumerable<double> confidences,
            IEnumerable<AlignedWordPair> alignedWordPairs
        )
        {
            SourceTokens = sourceTokens.ToArray();
            TargetTokens = targetTokens.ToArray();
            Confidences = confidences.ToArray();
            AlignedWordPairs = alignedWordPairs;
        }

        public IReadOnlyList<string> SourceTokens { get; }
        public IReadOnlyList<string> TargetTokens { get; }
        public IReadOnlyList<double> Confidences { get; }
        public IEnumerable<AlignedWordPair> AlignedWordPairs { get; }
    }
}
