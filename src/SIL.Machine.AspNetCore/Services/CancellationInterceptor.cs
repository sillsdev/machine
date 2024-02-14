namespace SIL.Machine.AspNetCore.Services;

public class CancellationInterceptor(ILogger<CancellationInterceptor> logger) : Interceptor
{
    private readonly ILogger<CancellationInterceptor> _logger = logger;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                _logger.LogInformation("An operation was canceled.");
                throw new RpcException(new Status(StatusCode.Cancelled, "An operation was canceled."));
            }
            else
            {
                throw;
            }
        }
    }
}
