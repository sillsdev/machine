namespace SIL.Machine.WebApi
{
    public class HttpResponse
    {
        public HttpResponse(bool isSuccess, int statusCode, string content = null)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Content = content;
        }

        public bool IsSuccess { get; }
        public int StatusCode { get; }
        public string Content { get; }
    }
}
