using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
    public class TranslationEngineCorpusDto
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public string Href { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public ResourceDto Corpus { get; set; }
        public bool Pretranslate { get; set; }
    }
}
