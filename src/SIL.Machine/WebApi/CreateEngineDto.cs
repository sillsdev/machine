using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class CreateEngineDto
	{
		[JsonProperty(Required = Required.Always)]
		public string SourceLanguageTag { get; set; }
		[JsonProperty(Required = Required.Always)]
		public string TargetLanguageTag { get; set; }
		[JsonProperty(Required = Required.Always)]
		public EngineType Type { get; set; }
	}
}
