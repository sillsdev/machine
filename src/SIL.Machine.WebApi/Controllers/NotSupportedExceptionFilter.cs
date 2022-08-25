namespace SIL.Machine.WebApi.Controllers;

public class NotSupportedExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (
            context.Exception is NotSupportedException
            || (context.Exception is RpcException rpcException && rpcException.StatusCode == StatusCode.Unimplemented)
        )
        {
            context.ExceptionHandled = true;
            context.Result = new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
        }
    }
}
