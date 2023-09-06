namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineState : AsyncDisposableBase
{
    private readonly ISmtModelFactory _smtModelFactory;
    private readonly ITransferEngineFactory _transferEngineFactory;
    private readonly ITruecaserFactory _truecaserFactory;
    private readonly AsyncLock _lock = new();

    private IInteractiveTranslationModel? _smtModel;
    private HybridTranslationEngine? _hybridEngine;

    public SmtTransferEngineState(
        ISmtModelFactory smtModelFactory,
        ITransferEngineFactory transferEngineFactory,
        ITruecaserFactory truecaserFactory,
        string engineId
    )
    {
        _smtModelFactory = smtModelFactory;
        _transferEngineFactory = transferEngineFactory;
        _truecaserFactory = truecaserFactory;
        EngineId = engineId;
    }

    public string EngineId { get; }

    public bool IsUpdated { get; set; }
    public int CurrentBuildRevision { get; set; } = -1;
    public DateTime LastUsedTime { get; set; } = DateTime.UtcNow;
    public bool IsLoaded => _hybridEngine != null;

    public void InitNew()
    {
        _smtModelFactory.InitNew(EngineId);
        _transferEngineFactory.InitNew(EngineId);
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
                var tokenizer = new LatinWordTokenizer();
                var detokenizer = new LatinWordDetokenizer();
                var truecaser = await _truecaserFactory.CreateAsync(EngineId);
                _smtModel = _smtModelFactory.Create(EngineId, tokenizer, detokenizer, truecaser);
                var transferEngine = _transferEngineFactory.Create(EngineId, tokenizer, detokenizer, truecaser);
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
        _smtModelFactory.Cleanup(EngineId);
        _transferEngineFactory.Cleanup(EngineId);
        _truecaserFactory.Cleanup(EngineId);
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
