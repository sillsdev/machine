namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineCommitService(
    IServiceProvider services,
    IOptionsMonitor<SmtTransferEngineOptions> engineOptions,
    SmtTransferEngineStateService stateService,
    ILogger<SmtTransferEngineCommitService> logger
)
    : RecurrentTask(
        "SMT transfer engine commit service",
        services,
        engineOptions.CurrentValue.EngineCommitFrequency,
        logger
    )
{
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _engineOptions = engineOptions;
    private readonly SmtTransferEngineStateService _stateService = stateService;
    private readonly ILogger<SmtTransferEngineCommitService> _logger = logger;

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
