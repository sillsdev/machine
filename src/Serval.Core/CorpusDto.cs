using Newtonsoft.Json;

namespace Serval.Core
{
    public class CorpusDto : ResourceDto
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public string Name { get; set; }
        public CorpusType Type { get; set; }
        public FileFormat Format { get; set; }
    }
}
