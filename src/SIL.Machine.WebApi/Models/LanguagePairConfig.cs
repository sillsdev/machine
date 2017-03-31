using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public class LanguagePairConfig
	{
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public IReadOnlyCollection<ProjectConfig> Projects { get; set; }
	}
}
