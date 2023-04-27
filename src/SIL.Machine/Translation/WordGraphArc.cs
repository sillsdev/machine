using SIL.Machine.Annotations;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public class WordGraphArc
    {
        private readonly double[] _confidences;

        public WordGraphArc(
            int prevState,
            int nextState,
            double score,
            IEnumerable<string> words,
            WordAlignmentMatrix alignment,
            Range<int> sourceSegmentRange,
            IEnumerable<TranslationSources> sources,
            IEnumerable<double> confidences = null
        )
        {
            PrevState = prevState;
            NextState = nextState;
            Score = score;
            Words = words.ToArray();
            Alignment = alignment;
            SourceSegmentRange = sourceSegmentRange;
            Sources = sources.ToArray();
            if (confidences == null)
                _confidences = Enumerable.Repeat(-1.0, Words.Count).ToArray();
            else
                _confidences = confidences.ToArray();
        }

        public int PrevState { get; }
        public int NextState { get; }
        public double Score { get; }
        public IReadOnlyList<string> Words { get; }
        public WordAlignmentMatrix Alignment { get; }
        public IReadOnlyList<double> Confidences => _confidences;
        public Range<int> SourceSegmentRange { get; }
        public IReadOnlyList<TranslationSources> Sources { get; }
        public bool IsUnknown => Sources.All(s => s == TranslationSources.None);

        internal void SetConfidence(int i, double score)
        {
            _confidences[i] = score;
        }
    }
}
