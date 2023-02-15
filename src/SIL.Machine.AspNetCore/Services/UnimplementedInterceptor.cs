namespace SIL.Machine.AspNetCore.Services;

public class UnimplementedInterceptor : Interceptor
{
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
        catch (NotSupportedException)
        {
            throw new RpcException(
                new Status(StatusCode.Unimplemented, "The call is not supported by the specified engine.")
            );
        }
    }
}
