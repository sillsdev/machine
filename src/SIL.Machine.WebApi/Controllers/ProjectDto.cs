namespace SIL.Machine.WebApi.Controllers
{
	public class ProjectDto : LinkDto
	{
		public string Id { get; set; }
		public bool IsShared { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public LinkDto Engine { get; set; }
	}
}
