using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bridge.Html5;

namespace SIL.Machine.Web
{
	public class MockHttpClient : IHttpClient
	{
		public IList<MockRequest> Requests { get; } = new List<MockRequest>();

		public string BaseUrl { get; set; }

		public Task<HttpResponse> SendAsync(HttpRequestMethod method, string url, string body = null, string contentType = null)
		{
			MockRequest request = Requests.FirstOrDefault(r => r.Method == method && (r.Url == null || r.Url == url) && (r.Body == null || r.Body == body));
			HttpResponse response;
			if (request != null)
			{
				request.CheckBody?.Invoke(body);
				response = request.ResponseText == null ? new HttpResponse(false, request.ErrorStatus) : new HttpResponse(true, 200, request.ResponseText);
			}
			else
			{
				response = new HttpResponse(false, 404);
			}
			return Task.FromResult(response);
		}

		public object ParseJson(string json)
		{
			return JSON.Parse(json);
		}

		public string ToJson(object obj)
		{
			return JSON.Stringify(obj);
		}
	}
}
