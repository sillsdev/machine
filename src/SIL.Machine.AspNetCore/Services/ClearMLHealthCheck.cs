public class ClearMLHealthCheck : IHealthCheck
{
    private readonly IClearMLService _clearMLService;
    private readonly ILogger _logger;

    public ClearMLHealthCheck(IClearMLService clearMLService, ILoggerFactory loggerFactory)
    {
        _clearMLService = clearMLService;
        _logger = loggerFactory.CreateLogger<S3HealthCheck>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            bool success = await _clearMLService.PingAsync();
            if (!success)
                return HealthCheckResult.Unhealthy("ClearML is unresponsive");
            bool workersAvailable = await _clearMLService.AvailableWorkersExist();
            if (!workersAvailable)
                return HealthCheckResult.Unhealthy("No ClearML agents are available");
            return HealthCheckResult.Healthy("ClearML is available");
        }
        catch (Exception e)
        {
            _logger.LogError(
                "ClearML is not available. The following exception occurred while pinging ClearML " + e.Message
            );
            return HealthCheckResult.Unhealthy(
                "ClearML is not available. The following exception occurred while pinging ClearML " + e.Message
            );
        }
    }
}
