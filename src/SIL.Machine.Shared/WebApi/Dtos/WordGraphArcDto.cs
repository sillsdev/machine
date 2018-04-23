namespace SIL.Machine.WebApi.Dtos
{
	public class WordGraphArcDto
	{
		public int PrevState { get; set; }
		public int NextState { get; set; }
		public float Score { get; set; }
		public string[] Words { get; set; }
		public float[] Confidences { get; set; }
		public RangeDto SourceSegmentRange { get; set; }
		public bool IsUnknown { get; set; }
		public AlignedWordPairDto[] Alignment { get; set; }
	}
}
