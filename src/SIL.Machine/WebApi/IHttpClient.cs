using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.WebApi
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
        Task<HttpResponse> SendAsync(
            HttpRequestMethod method,
            string url,
            string body,
            string contentType,
            CancellationToken ct = default(CancellationToken)
        );
    }
}
