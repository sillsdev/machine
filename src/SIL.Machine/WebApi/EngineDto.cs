namespace SIL.Machine.WebApi
{
	public class EngineDto : ResourceDto
	{
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public bool IsShared { get; set; }
		public ResourceDto[] Projects { get; set; }
		public double Confidence { get; set; }
		public int TrainedSegmentCount { get; set; }
	}
}
