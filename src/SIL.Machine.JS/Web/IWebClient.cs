using System;

namespace SIL.Machine.Web
{
	public interface IWebClient
	{
		void Send(string method, string url, string body = null, string contentType = null, Action<string> onSuccess = null, Action<int> onError = null);
	}
}
