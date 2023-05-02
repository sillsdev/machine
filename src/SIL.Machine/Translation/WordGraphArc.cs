using SIL.Machine.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class WordGraphArc
    {
        public WordGraphArc(
            int prevState,
            int nextState,
            double score,
            IEnumerable<string> targetTokens,
            WordAlignmentMatrix alignment,
            Range<int> sourceSegmentRange,
            IEnumerable<TranslationSources> sources,
            IEnumerable<double> confidences
        )
        {
            PrevState = prevState;
            NextState = nextState;
            Score = score;
            TargetTokens = targetTokens.ToArray();
            Alignment = alignment;
            SourceSegmentRange = sourceSegmentRange;
            Sources = sources.ToArray();
            Confidences = confidences.ToArray();
        }

        public int PrevState { get; }
        public int NextState { get; }
        public double Score { get; }
        public IReadOnlyList<string> TargetTokens { get; }
        public WordAlignmentMatrix Alignment { get; }
        public IReadOnlyList<double> Confidences { get; }
        public Range<int> SourceSegmentRange { get; }
        public IReadOnlyList<TranslationSources> Sources { get; }
        public bool IsUnknown => Sources.All(s => s == TranslationSources.None);
    }
}
