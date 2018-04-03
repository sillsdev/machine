using System;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Html5;

namespace SIL.Machine.WebApi.Client
{
	public class AjaxHttpClient : IHttpClient
	{
		public string BaseUrl { get; set; }

		public Task<HttpResponse> SendAsync(HttpRequestMethod method, string url, string body, string contentType,
			CancellationToken ct)
		{
			var tcs = new TaskCompletionSource<HttpResponse>();
			var request = new XMLHttpRequest();
			request.OnReadyStateChange = () =>
			{
				if (request.ReadyState != AjaxReadyState.Done)
					return;

				if ((request.Status >= 200 && request.Status < 300) || request.Status == 304)
					tcs.TrySetResult(new HttpResponse(true, request.Status, request.ResponseText));
				else
					tcs.TrySetResult(new HttpResponse(false, request.Status));
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
			if (!string.IsNullOrEmpty(contentType))
				request.SetRequestHeader("Content-Type", contentType);
			if (string.IsNullOrEmpty(body))
				request.Send();
			else
				request.Send(body);

			CancellationTokenRegistration reg = ct.Register(() =>
			{
				if (tcs.TrySetCanceled())
					request.Abort();
			});
			tcs.Task.ContinueWith(_ => reg.Dispose());
			return tcs.Task;
		}
	}
}
