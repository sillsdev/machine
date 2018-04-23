using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SIL.Machine.WebApi.Client
{
	public abstract class RestClientBase
	{
		public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};

		public RestClientBase(string baseUrl, IHttpClient httpClient)
		{
			HttpClient = httpClient;
			if (!baseUrl.EndsWith("/"))
				baseUrl += "/";
			HttpClient.BaseUrl = baseUrl;
		}

		public string BaseUrl => HttpClient.BaseUrl;
		public IHttpClient HttpClient { get; }
	}
}
