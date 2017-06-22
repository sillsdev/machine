using System;
using System.Threading.Tasks;
using Bridge.Html5;

namespace SIL.Machine.WebApi.Client
{
	public class AjaxHttpClient : IHttpClient
	{
		public string BaseUrl { get; set; }

		public Task<HttpResponse> SendAsync(HttpRequestMethod method, string url, string body = null, string contentType = null)
		{
			var tcs = new TaskCompletionSource<HttpResponse>();
			var request = new XMLHttpRequest();
			request.OnReadyStateChange = () =>
			{
				if (request.ReadyState != AjaxReadyState.Done)
					return;

				if ((request.Status >= 200 && request.Status < 300) || request.Status == 304)
					tcs.SetResult(new HttpResponse(true, request.Status, request.ResponseText));
				else
					tcs.SetResult(new HttpResponse(false, request.Status));
			};

			string methodStr;
			switch (method)
			{
				case HttpRequestMethod.Get:
					methodStr = "GET";
					break;
				case HttpRequestMethod.Post:
					methodStr = "POST";
					break;
				case HttpRequestMethod.Delete:
					methodStr = "DELETE";
					break;
				case HttpRequestMethod.Put:
					methodStr = "PUT";
					break;
				default:
					throw new ArgumentException("Unrecognized HTTP method.", nameof(method));
			}

			request.Open(methodStr, BaseUrl + url);
			if (contentType != null)
				request.SetRequestHeader("Content-Type", contentType);
			if (body == null)
				request.Send();
			else
				request.Send(body);
			return tcs.Task;
		}
	}
}
