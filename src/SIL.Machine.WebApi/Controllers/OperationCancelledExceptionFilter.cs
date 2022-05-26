namespace SIL.Machine.WebApi.Controllers;

public class OperationCancelledExceptionFilter : ExceptionFilterAttribute
{
    private readonly ILogger _logger;

    public OperationCancelledExceptionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OperationCancelledExceptionFilter>();
    }

    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException)
        {
            _logger.LogInformation("Request was cancelled");
            context.ExceptionHandled = true;
            context.Result = new StatusCodeResult(499);
        }
    }
}
