using Newtonsoft.Json;

namespace Serval.Core
{
    public class TranslationEngineCorpusConfigDto
    {
        /// <summary>
        /// The corpus id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string CorpusId { get; set; }

        /// <summary>
        /// Indicates whether to generate pretranslations for untranslated segments in the corpus.
        /// </summary>
        public bool? Pretranslate { get; set; }
    }
}
