using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.WebApi
{
    public class StandardHttpClient : IHttpClient
    {
        private readonly HttpClient _client;

        public StandardHttpClient()
        {
            _client = new HttpClient();
        }

        public string BaseUrl
        {
            get => _client.BaseAddress.ToString();
            set => _client.BaseAddress = new Uri(value);
        }

        public async Task<HttpResponse> SendAsync(
            HttpRequestMethod method,
            string url,
            string body,
            string contentType,
            CancellationToken cancellationToken = default
        )
        {
            HttpMethod httpMethod;
            switch (method)
            {
                case HttpRequestMethod.Get:
                    httpMethod = HttpMethod.Get;
                    break;
                case HttpRequestMethod.Post:
                    httpMethod = HttpMethod.Post;
                    break;
                case HttpRequestMethod.Put:
                    httpMethod = HttpMethod.Put;
                    break;
                case HttpRequestMethod.Delete:
                    httpMethod = HttpMethod.Delete;
                    break;
                default:
                    throw new ArgumentException("The specified method is unrecognized.", nameof(method));
            }
            var request = new HttpRequestMessage(httpMethod, url);
            if (!string.IsNullOrEmpty(body))
            {
                request.Content = string.IsNullOrEmpty(contentType)
                  ? new StringContent(body, Encoding.UTF8)
                  : new StringContent(body, Encoding.UTF8, contentType);
            }
            HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new HttpResponse(false, (int)response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            return new HttpResponse(true, (int)response.StatusCode, content);
        }
    }
}
