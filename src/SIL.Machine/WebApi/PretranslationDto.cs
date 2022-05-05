using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class PretranslationDto
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public string TextId { get; set; }
		[JsonProperty(Required = Required.DisallowNull)]
		public string[] Refs { get; set; }
		[JsonProperty(Required = Required.DisallowNull)]
		public string Text { get; set; }
	}
}
