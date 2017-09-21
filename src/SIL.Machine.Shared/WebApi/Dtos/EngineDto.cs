namespace SIL.Machine.WebApi.Dtos
{
	public class EngineDto : ResourceDto
	{
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public bool IsShared { get; set; }
		public ResourceDto[] Projects { get; set; }
	}
}
