using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
    public abstract class WordAlignerBase : IWordAligner
    {
        public abstract WordAlignmentMatrix Align(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        );

        public IReadOnlyList<WordAlignmentMatrix> AlignBatch(
            IReadOnlyList<(IReadOnlyList<string> SourceSegment, IReadOnlyList<string> TargetSegment)> segments
        )
        {
            return segments.AsParallel().AsOrdered().Select(s => Align(s.SourceSegment, s.TargetSegment)).ToArray();
        }
    }
}
