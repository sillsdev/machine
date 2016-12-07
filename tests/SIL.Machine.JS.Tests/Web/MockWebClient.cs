using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Web;

namespace SIL.Machine.JS.Tests.Web
{
	public class MockWebClient : IWebClient
	{
		public IList<MockRequest> Requests { get; } = new List<MockRequest>();

		public void Send(string method, string url, string body = null, string contentType = null, Action<string> onSuccess = null, Action<int> onError = null)
		{
			MockRequest request = Requests.FirstOrDefault(r => (r.Method == null || r.Method == method) && (r.Url == null || r.Url == url) && (r.Body == null || r.Body == body));
			if (request != null)
			{
				request.CheckBody?.Invoke(body);

				if (request.ResponseText != null)
					onSuccess?.Invoke(request.ResponseText);
				else
					onError?.Invoke(request.ErrorStatus);
			}
			else
			{
				onError?.Invoke(404);
			}
		}
	}
}
