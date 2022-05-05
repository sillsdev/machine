using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class DataFileDto : ResourceDto
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public ResourceDto Corpus { get; set; }
		[JsonProperty(Required = Required.DisallowNull)]
		public string LanguageTag { get; set; }
		[JsonProperty(Required = Required.DisallowNull)]
		public string Name { get; set; }
		public string TextId { get; set; }
	}
}
