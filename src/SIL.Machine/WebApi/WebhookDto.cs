using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class WebhookDto : ResourceDto
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public string Url { get; set; }
		[JsonProperty(Required = Required.DisallowNull)]
		public WebhookEvent[] Events { get; set; }
	}
}
