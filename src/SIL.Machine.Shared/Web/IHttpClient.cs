using System.Threading.Tasks;

namespace SIL.Machine.Web
{
	public enum HttpRequestMethod
	{
		Get,
		Post,
		Put,
		Delete
	}

	public interface IHttpClient
	{
		string BaseUrl { get; set; }
		Task<HttpResponse> SendAsync(HttpRequestMethod method, string url, string body = null, string contentType = null);
		object ParseJson(string json);
		string ToJson(object obj);
	}
}
