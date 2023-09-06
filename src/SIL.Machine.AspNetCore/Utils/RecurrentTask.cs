namespace SIL.Machine.AspNetCore.Utils;

public abstract class RecurrentTask : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly TimeSpan _period;

    protected RecurrentTask(IServiceProvider services, TimeSpan period)
    {
        _services = services;
        _period = period;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_period == TimeSpan.Zero)
            return;

        using PeriodicTimer timer = new(_period);

        Started();

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using IServiceScope scope = _services.CreateScope();
                await DoWorkAsync(scope, stoppingToken);
            }
        }
        catch (OperationCanceledException) { }

        Stopped();
    }

    protected abstract void Started();
    protected abstract void Stopped();

    protected abstract Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken);
}
