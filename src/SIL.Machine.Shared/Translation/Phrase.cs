using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
	public class Phrase
	{
		public Phrase(Range<int> sourceSegmentRange, Range<int> targetSegmentRange)
		{
			SourceSegmentRange = sourceSegmentRange;
			TargetSegmentRange = targetSegmentRange;
		}

		public Range<int> SourceSegmentRange { get; }
		public Range<int> TargetSegmentRange { get; }
	}
}
