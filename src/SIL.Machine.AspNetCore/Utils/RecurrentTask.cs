namespace SIL.Machine.AspNetCore.Utils;

public abstract class RecurrentTask(
    string serviceName,
    IServiceProvider services,
    TimeSpan period,
    ILogger<RecurrentTask> logger,
    bool enable = true
) : BackgroundService
{
    private readonly bool _enable = enable;
    private readonly string _serviceName = serviceName;
    private readonly IServiceProvider _services = services;
    private readonly TimeSpan _period = period;
    private readonly ILogger<RecurrentTask> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enable)
            return;

        using PeriodicTimer timer = new(_period);

        _logger.LogInformation($"{_serviceName} started.");

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using IServiceScope scope = _services.CreateScope();
                await DoWorkAsync(scope, stoppingToken);
            }
        }
        catch (OperationCanceledException) { }

        _logger.LogInformation($"{_serviceName} stopped.");
    }

    protected abstract Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken);
}
