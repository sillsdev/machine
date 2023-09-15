public class S3HealthCheck : IHealthCheck
{
    private readonly ISharedFileService _sharedFileService;
    private readonly ILogger _logger;

    public S3HealthCheck(ISharedFileService sharedFileService, ILoggerFactory loggerFactory)
    {
        _sharedFileService = sharedFileService;
        _logger = loggerFactory.CreateLogger<S3HealthCheck>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        int numConsecutiveFailures = 0;
        Exception exception = new Exception();
        while (numConsecutiveFailures < 3)
        {
            try
            {
                await _sharedFileService.Ls("/models/");
                return HealthCheckResult.Healthy("The S3 bucket is available");
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    $"S3 bucket is not responding. Pinging again in 30 seconds. Retry count: {numConsecutiveFailures}"
                );
                numConsecutiveFailures++;
                exception = e;
                await Task.Delay(60_000);
            }
        }
        return ReturnUnhealthyStatus(exception);
    }

    private HealthCheckResult ReturnUnhealthyStatus(Exception e)
    {
        if (
            e is HttpRequestException httpRequestException
            && httpRequestException.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
        )
        {
            return HealthCheckResult.Unhealthy(
                "S3 bucket is not available because of an authentication error. Please verify that credentials are valid."
            );
        }
        return HealthCheckResult.Unhealthy(
            "S3 bucket is not available. The following exception occurred: " + e.Message
        );
    }
}
