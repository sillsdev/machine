using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class NewCorpusDto
	{
		[JsonProperty(Required = Required.Always)]
		public string Name { get; set; }
		[JsonProperty(Required = Required.Always)]
		public CorpusType Type { get; set; }
		[JsonProperty(Required = Required.Always)]
		public FileFormat Format { get; set; }
	}
}
