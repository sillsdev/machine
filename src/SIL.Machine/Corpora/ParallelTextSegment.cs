namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(TextSegmentRef segRef, string sourceValue, string targetValue)
		{
			SegmentRef = segRef;
			SourceValue = sourceValue;
			TargetValue = targetValue;
		}

		public TextSegmentRef SegmentRef { get; }

		public bool IsEmpty => string.IsNullOrEmpty(SourceValue) || string.IsNullOrEmpty(TargetValue);

		public string SourceValue { get; }

		public string TargetValue { get; }
	}
}
