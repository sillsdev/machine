using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
    public class Phrase
    {
        public Phrase(Range<int> sourceSegmentRange, int targetSegmentCut)
        {
            SourceSegmentRange = sourceSegmentRange;
            TargetSegmentCut = targetSegmentCut;
        }

        public Range<int> SourceSegmentRange { get; }
        public int TargetSegmentCut { get; }
    }
}
