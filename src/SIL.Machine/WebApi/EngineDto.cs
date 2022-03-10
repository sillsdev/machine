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
		public EngineType Type { get; set; }
		public bool IsBuilding { get; set; }
		public int ModelRevision { get; set; }
		public double Confidence { get; set; }
		public int TrainedSegmentCount { get; set; }
	}
}
