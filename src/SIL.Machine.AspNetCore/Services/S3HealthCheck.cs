public class S3HealthCheck : IHealthCheck
{
    private readonly ISharedFileService _sharedFileService;
    private readonly ILogger _logger;
    private int _numConsecutiveFailures;
    private readonly AsyncLock _lock;

    public S3HealthCheck(ISharedFileService sharedFileService, ILogger<S3HealthCheck> logger)
    {
        _sharedFileService = sharedFileService;
        _logger = logger;
        _numConsecutiveFailures = 0;
        _lock = new AsyncLock();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation($"{DateTime.Now}");
            await _sharedFileService.Ls("/models/");
            return HealthCheckResult.Healthy("The S3 bucket is available");
        }
        catch (Exception e)
        {
            using (await _lock.LockAsync())
                _numConsecutiveFailures++;
            if (
                e is HttpRequestException httpRequestException
                && httpRequestException.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
            )
            {
                using (await _lock.LockAsync())
                    return _numConsecutiveFailures > 3
                        ? HealthCheckResult.Unhealthy(
                            "S3 bucket is not available because of an authentication error. Please verify that credentials are valid."
                        )
                        : HealthCheckResult.Degraded(
                            "S3 bucket is not available because of an authentication error. Please verify that credentials are valid."
                        );
            }
            using (await _lock.LockAsync())
                return _numConsecutiveFailures > 3
                    ? HealthCheckResult.Unhealthy(
                        "S3 bucket is not available. The following exception occurred: " + e.Message
                    )
                    : HealthCheckResult.Degraded(
                        "S3 bucket is not available. The following exception occurred: " + e.Message
                    );
        }
    }
}
