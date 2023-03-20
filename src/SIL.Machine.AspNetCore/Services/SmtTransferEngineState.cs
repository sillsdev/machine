namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineState : AsyncDisposableBase
{
    private readonly ISmtModelFactory _smtModelFactory;
    private readonly ITransferEngineFactory _transferEngineFactory;
    private readonly ITruecaserFactory _truecaserFactory;

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
        SmtModel = new Lazy<IInteractiveTranslationModel>(CreateSmtModel);
        TransferEngine = new Lazy<ITranslationEngine?>(CreateTransferEngine);
        HybridEngine = new Lazy<HybridTranslationEngine>(CreateHybridEngine);
        Truecaser = new AsyncLazy<ITruecaser>(CreateTruecaserAsync);
    }

    public string EngineId { get; }
    public Lazy<IInteractiveTranslationModel> SmtModel { get; private set; }
    public Lazy<ITranslationEngine?> TransferEngine { get; private set; }
    public Lazy<HybridTranslationEngine> HybridEngine { get; private set; }
    public AsyncLazy<ITruecaser> Truecaser { get; private set; }
    public bool IsUpdated { get; set; }
    public int CurrentBuildRevision { get; set; } = -1;
    public DateTime LastUsedTime { get; set; } = DateTime.UtcNow;

    public bool IsLoaded => SmtModel.IsValueCreated;

    public void InitNew()
    {
        _smtModelFactory.InitNew(EngineId);
        _transferEngineFactory.InitNew(EngineId);
    }

    public async Task UnloadAsync()
    {
        if (!IsLoaded)
            return;

        await SaveModelAsync();

        SmtModel.Value.Dispose();
        TransferEngine.Value?.Dispose();

        SmtModel = new Lazy<IInteractiveTranslationModel>(CreateSmtModel);
        TransferEngine = new Lazy<ITranslationEngine?>(CreateTransferEngine);
        HybridEngine = new Lazy<HybridTranslationEngine>(CreateHybridEngine);
        Truecaser = new AsyncLazy<ITruecaser>(CreateTruecaserAsync);
        CurrentBuildRevision = -1;
    }

    public async Task SaveModelAsync()
    {
        if (IsUpdated)
        {
            await SmtModel.Value.SaveAsync();
            ITruecaser truecaser = await Truecaser;
            await truecaser.SaveAsync();
            IsUpdated = false;
        }
    }

    public async Task DeleteDataAsync()
    {
        await UnloadAsync();
        _smtModelFactory.Cleanup(EngineId);
        _transferEngineFactory.Cleanup(EngineId);
        _truecaserFactory.Cleanup(EngineId);
    }

    private IInteractiveTranslationModel CreateSmtModel()
    {
        return _smtModelFactory.Create(EngineId);
    }

    private ITranslationEngine? CreateTransferEngine()
    {
        return _transferEngineFactory.Create(EngineId);
    }

    private HybridTranslationEngine CreateHybridEngine()
    {
        IInteractiveTranslationEngine interactiveEngine = SmtModel.Value;
        ITranslationEngine? transferEngine = TransferEngine.Value;
        return new HybridTranslationEngine(interactiveEngine, transferEngine);
    }

    private Task<ITruecaser> CreateTruecaserAsync()
    {
        return _truecaserFactory.CreateAsync(EngineId)!;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await UnloadAsync();
    }
}
