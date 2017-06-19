using System.Collections.Generic;

namespace SIL.Machine.WebApi.Controllers
{
	public class EngineDto : LinkDto
	{
		public string Id { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public bool IsShared { get; set; }
		public IReadOnlyCollection<LinkDto> Projects { get; set; }
	}
}
