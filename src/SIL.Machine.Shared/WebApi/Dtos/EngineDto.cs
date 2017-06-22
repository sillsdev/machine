namespace SIL.Machine.WebApi.Dtos
{
	public class EngineDto : LinkDto
	{
		public string Id { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public bool IsShared { get; set; }
		public LinkDto[] Projects { get; set; }
	}
}
