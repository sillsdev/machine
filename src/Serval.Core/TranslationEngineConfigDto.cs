using Newtonsoft.Json;

namespace Serval.Core
{
    public class TranslationEngineConfigDto
    {
        /// <summary>
        /// The translation engine name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The source language tag.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SourceLanguageTag { get; set; }

        /// <summary>
        /// The target language tag.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string TargetLanguageTag { get; set; }

        /// <summary>
        /// The translation engine type.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Type { get; set; }
    }
}
