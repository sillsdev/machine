public class ClearMLHealthCheck : IHealthCheck
{
    private readonly IClearMLService _clearMLService;
    private readonly ILogger _logger;

    public ClearMLHealthCheck(IClearMLService clearMLService, ILoggerFactory loggerFactory)
    {
        _clearMLService = clearMLService;
        _logger = loggerFactory.CreateLogger<ClearMLHealthCheck>();
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
            bool workersAvailable = await _clearMLService.WorkersAreAssignedToQueue();
            if (!workersAvailable)
                return HealthCheckResult.Unhealthy("No ClearML agents are available");
            return HealthCheckResult.Healthy("ClearML is available");
        }
        catch (Exception e)
        {
            _logger.LogError(0, exception: e, null);
            return HealthCheckResult.Unhealthy(exception: e);
        }
    }
}
