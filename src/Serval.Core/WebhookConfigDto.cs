using Newtonsoft.Json;

namespace Serval.Core
{
    public class WebhookConfigDto
    {
        /// <summary>
        /// The callback URL.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Url { get; set; }

        /// <summary>
        /// The shared secret.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Secret { get; set; }

        /// <summary>
        /// The webhook events.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public WebhookEvent[] Events { get; set; }
    }
}
