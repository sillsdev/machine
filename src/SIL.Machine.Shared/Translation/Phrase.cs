using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
	public class Phrase
	{
		public Phrase(Range<int> sourceSegmentRange, int targetSegmentCut, double confidence)
		{
			SourceSegmentRange = sourceSegmentRange;
			TargetSegmentCut = targetSegmentCut;
			Confidence = confidence;
		}

		public Range<int> SourceSegmentRange { get; }
		public int TargetSegmentCut { get; }
		public double Confidence { get; }
	}
}
