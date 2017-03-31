using System.Collections.Generic;

namespace SIL.Machine.WebApi.Services
{
	public class LanguagePairDto
	{
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public IReadOnlyCollection<ProjectDto> Projects { get; set; }
	}
}
