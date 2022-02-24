using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class EngineDto : ResourceDto
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public string SourceLanguageTag { get; set; }
		[JsonProperty(Required = Required.DisallowNull)]
		public string TargetLanguageTag { get; set; }
		[JsonProperty(Required = Required.DisallowNull)]
		public string Type { get; set; }
		public double Confidence { get; set; }
		public int TrainedSegmentCount { get; set; }
	}
}
