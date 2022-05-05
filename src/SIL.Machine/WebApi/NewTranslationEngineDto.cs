using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class NewTranslationEngineDto
	{
		[JsonProperty(Required = Required.Always)]
		public string SourceLanguageTag { get; set; }
		[JsonProperty(Required = Required.Always)]
		public string TargetLanguageTag { get; set; }
		[JsonProperty(Required = Required.Always)]
		public TranslationEngineType Type { get; set; }
	}
}
