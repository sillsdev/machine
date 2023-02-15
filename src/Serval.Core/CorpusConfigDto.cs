using Newtonsoft.Json;

namespace Serval.Core
{
    public class CorpusConfigDto
    {
        /// <summary>
        /// The corpus name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The corpus type.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public CorpusType Type { get; set; }

        /// <summary>
        /// The format of all files in the corpus.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public FileFormat Format { get; set; }
    }
}
