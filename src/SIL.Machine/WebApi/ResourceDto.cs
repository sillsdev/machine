using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
    public class ResourceDto
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public string Id { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public string Href { get; set; }
    }
}
