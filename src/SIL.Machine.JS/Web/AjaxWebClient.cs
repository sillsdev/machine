using System;
using Bridge.Html5;

namespace SIL.Machine.Web
{
	public class AjaxWebClient : IWebClient
	{
		public void Send(string method, string url, string body = null, string contentType = null, Action<string> onSuccess = null, Action<int> onError = null)
		{
			var request = new XMLHttpRequest();
			if (onSuccess != null || onError != null)
			{
				request.OnReadyStateChange = () =>
				{
					if (request.ReadyState != AjaxReadyState.Done)
						return;

					if (request.Status == 200 || request.Status == 304)
					{
						onSuccess?.Invoke(request.ResponseText);
					}
					else
					{
						onError?.Invoke(request.Status);
					}
				};
			}

			request.Open(method, url);
			if (contentType != null)
				request.SetRequestHeader("Content-Type", contentType);
			if (body == null)
				request.Send();
			else
				request.Send(body);
		}
	}
}
