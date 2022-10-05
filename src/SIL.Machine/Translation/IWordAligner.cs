using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public interface IWordAligner
    {
        WordAlignmentMatrix Align(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment);
        IReadOnlyList<WordAlignmentMatrix> AlignBatch(
            IReadOnlyList<(IReadOnlyList<string> SourceSegment, IReadOnlyList<string> TargetSegment)> segments
        );
    }
}
