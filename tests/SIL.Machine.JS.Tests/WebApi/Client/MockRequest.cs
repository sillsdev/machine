using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.WebApi.Client
{
	public class MockRequest
	{
		public HttpRequestMethod Method { get; set; }
		public string Url { get; set; }
		public string Body { get; set; }
		public Func<string, CancellationToken, Task> Action { get; set; }
		public string ResponseText { get; set; }
		public int ErrorStatus { get; set; }
	}
}
