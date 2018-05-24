using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.WebApi.Client
{
	public class MockHttpClient : IHttpClient
	{
		public IList<MockRequest> Requests { get; } = new List<MockRequest>();

		public string BaseUrl { get; set; }

		public async Task<HttpResponse> SendAsync(HttpRequestMethod method, string url, string body, string contentType,
			CancellationToken ct = default(CancellationToken))
		{
			MockRequest request = Requests.FirstOrDefault(r => r.Method == method && (r.Url == null || r.Url == url)
				&& (r.Body == null || r.Body == body));
			HttpResponse response;
			if (request != null)
			{
				if (request.Action != null)
					await request.Action(body, ct);
				response = request.ResponseText == null
					? new HttpResponse(false, request.ErrorStatus)
					: new HttpResponse(true, 200, request.ResponseText);
			}
			else
			{
				response = new HttpResponse(false, 404);
			}
			return response;
		}
	}
}
