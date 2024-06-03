namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineState(
    ISmtModelFactory smtModelFactory,
    ITransferEngineFactory transferEngineFactory,
    ITruecaserFactory truecaserFactory,
    IOptionsMonitor<SmtTransferEngineOptions> options,
    string engineId
) : AsyncDisposableBase
{
    private readonly ISmtModelFactory _smtModelFactory = smtModelFactory;
    private readonly ITransferEngineFactory _transferEngineFactory = transferEngineFactory;
    private readonly ITruecaserFactory _truecaserFactory = truecaserFactory;
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _options = options;
    private readonly AsyncLock _lock = new();

    private IInteractiveTranslationModel? _smtModel;
    private HybridTranslationEngine? _hybridEngine;

    public string EngineId { get; } = engineId;

    public bool IsUpdated { get; set; }
    public int CurrentBuildRevision { get; set; } = -1;
    public DateTime LastUsedTime { get; set; } = DateTime.UtcNow;
    public bool IsLoaded => _hybridEngine != null;

    private string EngineDir => Path.Combine(_options.CurrentValue.EnginesDir, EngineId);

    public async Task InitNewAsync(CancellationToken cancellationToken = default)
    {
        await _smtModelFactory.InitNewAsync(EngineDir, cancellationToken);
        await _transferEngineFactory.InitNewAsync(EngineDir, cancellationToken);
    }

    public async Task<HybridTranslationEngine> GetHybridEngineAsync(int buildRevision)
    {
        using (await _lock.LockAsync())
        {
            if (_hybridEngine is not null && CurrentBuildRevision != -1 && buildRevision != CurrentBuildRevision)
            {
                IsUpdated = false;
                await UnloadAsync();
            }

            if (_hybridEngine is null)
            {
                LatinWordTokenizer tokenizer = new();
                LatinWordDetokenizer detokenizer = new();
                ITruecaser truecaser = await _truecaserFactory.CreateAsync(EngineDir);
                _smtModel = await _smtModelFactory.CreateAsync(EngineDir, tokenizer, detokenizer, truecaser);
                ITranslationEngine? transferEngine = await _transferEngineFactory.CreateAsync(
                    EngineDir,
                    tokenizer,
                    detokenizer,
                    truecaser
                );
                _hybridEngine = new HybridTranslationEngine(_smtModel, transferEngine)
                {
                    TargetDetokenizer = detokenizer
                };
            }
            CurrentBuildRevision = buildRevision;
            return _hybridEngine;
        }
    }

    public async Task DeleteDataAsync()
    {
        await UnloadAsync();
        await _smtModelFactory.CleanupAsync(EngineDir);
        await _transferEngineFactory.CleanupAsync(EngineDir);
        await _truecaserFactory.CleanupAsync(EngineDir);
    }

    public async Task CommitAsync(
        int buildRevision,
        TimeSpan inactiveTimeout,
        CancellationToken cancellationToken = default
    )
    {
        if (_hybridEngine is null)
            return;

        if (CurrentBuildRevision == -1)
            CurrentBuildRevision = buildRevision;
        if (buildRevision != CurrentBuildRevision)
        {
            await UnloadAsync(cancellationToken);
            CurrentBuildRevision = buildRevision;
        }
        else if (DateTime.Now - LastUsedTime > inactiveTimeout)
        {
            await UnloadAsync(cancellationToken);
        }
        else
        {
            await SaveModelAsync(cancellationToken);
        }
    }

    private async Task SaveModelAsync(CancellationToken cancellationToken = default)
    {
        if (_smtModel is not null && IsUpdated)
        {
            await _smtModel.SaveAsync(cancellationToken);
            IsUpdated = false;
        }
    }

    private async Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        if (_hybridEngine is null)
            return;

        await SaveModelAsync(cancellationToken);

        _hybridEngine.Dispose();

        _smtModel = null;
        _hybridEngine = null;
        CurrentBuildRevision = -1;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await UnloadAsync();
    }
}
