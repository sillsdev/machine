namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineCommitService : RecurrentTask
{
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _engineOptions;
    private readonly SmtTransferEngineStateService _stateService;
    private readonly ILogger<SmtTransferEngineCommitService> _logger;

    public SmtTransferEngineCommitService(
        IServiceProvider services,
        IOptionsMonitor<SmtTransferEngineOptions> engineOptions,
        SmtTransferEngineStateService stateService,
        ILogger<SmtTransferEngineCommitService> logger
    )
        : base("SMT transfer engine commit service", services, engineOptions.CurrentValue.EngineCommitFrequency, logger)
    {
        _engineOptions = engineOptions;
        _stateService = stateService;
        _logger = logger;
    }

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var engines = scope.ServiceProvider.GetRequiredService<IRepository<TranslationEngine>>();
            var lockFactory = scope.ServiceProvider.GetRequiredService<IDistributedReaderWriterLockFactory>();
            await _stateService.CommitAsync(
                lockFactory,
                engines,
                _engineOptions.CurrentValue.InactiveEngineTimeout,
                cancellationToken
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while committing SMT transfer engines.");
        }
    }
}
