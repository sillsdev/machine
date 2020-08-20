namespace SIL.Machine.WebApi
{
	public class SegmentPairDto
	{
		public string[] SourceSegment { get; set; }
		public string[] TargetSegment { get; set; }
		public bool SentenceStart { get; set; }
	}
}
