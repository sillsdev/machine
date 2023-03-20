namespace SIL.Machine.AspNetCore.Utils;

public class StartupTask : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly Func<IServiceProvider, CancellationToken, Task> _task;

    public StartupTask(IServiceProvider services, Func<IServiceProvider, CancellationToken, Task> task)
    {
        _services = services;
        _task = task;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _services.CreateScope();
        await _task(scope.ServiceProvider, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
