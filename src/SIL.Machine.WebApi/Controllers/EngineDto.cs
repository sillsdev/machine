using System.Collections.Generic;

namespace SIL.Machine.WebApi.Controllers
{
	public class EngineDto
	{
		public string Id { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public bool IsShared { get; set; }
		public IReadOnlyCollection<string> Projects { get; set; }
	}
}
