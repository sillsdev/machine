namespace SIL.Machine.WebApi.Controllers
{
	public class ProjectDto
	{
		public string Id { get; set; }
		public bool IsShared { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
	}
}
