using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class CreateWebhookDto
	{
		[JsonProperty(Required = Required.Always)]
		public string Url { get; set; }
		[JsonProperty(Required = Required.Always)]
		public string Secret { get; set; }
		[JsonProperty(Required = Required.Always)]
		public WebhookEvent[] Events { get; set; }
	}
}
