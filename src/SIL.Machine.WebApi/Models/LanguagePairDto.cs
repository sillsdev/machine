using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public class LanguagePairDto
	{
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public IReadOnlyList<ProjectDto> Projects { get; set; }
	}
}
