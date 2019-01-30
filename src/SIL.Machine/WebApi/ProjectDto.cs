namespace SIL.Machine.WebApi
{
	public class ProjectDto : ResourceDto
	{
		public string SourceSegmentType { get; set; }
		public string TargetSegmentType { get; set; }
		public bool IsShared { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public ResourceDto Engine { get; set; }
	}
}
