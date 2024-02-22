namespace SIL.Machine.AspNetCore.Services;

public class HangfireHealthCheck(JobStorage jobStorage, IOptions<BackgroundJobServerOptions> options) : IHealthCheck
{
    private readonly JobStorage _jobStorage = jobStorage;
    private readonly IOptions<BackgroundJobServerOptions> _options = options;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (
            _jobStorage
                .GetMonitoringApi()
                .Servers()
                .Any(s => (DateTime.UtcNow - s.Heartbeat) < _options.Value.ServerTimeout)
        )
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("There are no Hangfire servers running."));
    }
}
