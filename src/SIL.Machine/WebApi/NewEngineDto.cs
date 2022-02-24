using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class NewEngineDto
	{
		public string Id { get; set; }
		[JsonProperty(Required = Required.Always)]
		public string SourceLanguageTag { get; set; }
		[JsonProperty(Required = Required.Always)]
		public string TargetLanguageTag { get; set; }
		[JsonProperty(Required = Required.Always)]
		public string Type { get; set; }
	}
}
