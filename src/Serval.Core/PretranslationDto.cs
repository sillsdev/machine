using Newtonsoft.Json;

namespace Serval.Core
{
    public class PretranslationDto
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public string TextId { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public string[] Refs { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public string Translation { get; set; }
    }
}
