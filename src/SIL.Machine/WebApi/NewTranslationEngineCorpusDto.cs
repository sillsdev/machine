using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class NewTranslationEngineCorpusDto
	{
		[JsonProperty(Required = Required.Always)]
		public string CorpusId { get; set; }
		public bool? Pretranslate { get; set; }
	}
}
