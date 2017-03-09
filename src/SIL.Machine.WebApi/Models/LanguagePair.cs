using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Models
{
	public class LanguagePair
	{
		public LanguagePair(string sourceLanguageTag, string targetLanguageTag, string configDir)
		{
			SourceLanguageTag = sourceLanguageTag;
			TargetLanguageTag = targetLanguageTag;
			ConfigDirectory = configDir;
			Projects = new KeyedList<string, Project>(p => p.Id);
		}

		public string SourceLanguageTag { get; }
		public string TargetLanguageTag { get; }
		public string ConfigDirectory { get; }
		public IKeyedCollection<string, Project> Projects { get; }
		public Engine SharedEngine { get; set; }
	}
}
