using Newtonsoft.Json;

namespace Serval.Core
{
    public class WebhookDto : ResourceDto
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public string Url { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public WebhookEvent[] Events { get; set; }
    }
}
