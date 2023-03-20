namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineCommitService : DisposableBase, IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _engineOptions;
    private readonly SmtTransferEngineStateService _stateService;
    private readonly AsyncTimer _commitTimer;

    public SmtTransferEngineCommitService(
        IServiceProvider services,
        IOptionsMonitor<SmtTransferEngineOptions> engineOptions,
        SmtTransferEngineStateService stateService
    )
    {
        _services = services;
        _engineOptions = engineOptions;
        _stateService = stateService;
        _commitTimer = new AsyncTimer(EngineCommitAsync);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _commitTimer.Start(_engineOptions.CurrentValue.EngineCommitFrequency);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _commitTimer.StopAsync();
    }

    private async Task EngineCommitAsync()
    {
        using IServiceScope scope = _services.CreateScope();
        var engines = scope.ServiceProvider.GetRequiredService<IRepository<TranslationEngine>>();
        var lockFactory = scope.ServiceProvider.GetRequiredService<IDistributedReaderWriterLockFactory>();
        await _stateService.CommitAsync(lockFactory, engines, _engineOptions.CurrentValue.InactiveEngineTimeout);
    }

    protected override void DisposeManagedResources()
    {
        _commitTimer.Dispose();
    }
}
