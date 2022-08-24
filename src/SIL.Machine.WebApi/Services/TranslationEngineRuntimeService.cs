namespace SIL.Machine.WebApi.Services;

public class TranslationEngineRuntimeService : AsyncDisposableBase, ITranslationEngineRuntimeService
{
    private readonly IOptionsMonitor<TranslationEngineOptions> _translationEngineOptions;
    private readonly ConcurrentDictionary<string, ITranslationEngineRuntime> _runtimes;
    private readonly AsyncTimer _commitTimer;
    private readonly Dictionary<TranslationEngineType, ITranslationEngineRuntimeFactory> _engineRuntimeFactories;

    public TranslationEngineRuntimeService(
        IOptionsMonitor<TranslationEngineOptions> translationEngineOptions,
        IEnumerable<ITranslationEngineRuntimeFactory> engineRuntimeFactories
    )
    {
        _translationEngineOptions = translationEngineOptions;
        _runtimes = new ConcurrentDictionary<string, ITranslationEngineRuntime>();
        _commitTimer = new AsyncTimer(EngineCommitAsync);
        _engineRuntimeFactories = engineRuntimeFactories.ToDictionary(f => f.Type);
    }

    public void Init()
    {
        _commitTimer.Start(_translationEngineOptions.CurrentValue.EngineCommitFrequency);
    }

    private async Task EngineCommitAsync()
    {
        foreach (ITranslationEngineRuntime runtime in _runtimes.Values)
            await runtime.CommitAsync();
    }

    public async Task CreateAsync(TranslationEngine engine)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = CreateRuntime(engine);
        await runtime.InitNewAsync();
    }

    public async Task DeleteAsync(TranslationEngine engine)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
        _runtimes.TryRemove(engine.Id, out _);
        await runtime.DeleteDataAsync();
        await runtime.DisposeAsync();
    }

    public async Task<TranslationResult?> TranslateAsync(TranslationEngine engine, IReadOnlyList<string> segment)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
        return await runtime.TranslateAsync(segment);
    }

    public async Task<IEnumerable<TranslationResult>?> TranslateAsync(
        TranslationEngine engine,
        int n,
        IReadOnlyList<string> segment
    )
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
        return await runtime.TranslateAsync(n, segment);
    }

    public async Task<WordGraph?> GetWordGraphAsync(TranslationEngine engine, IReadOnlyList<string> segment)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
        return await runtime.GetWordGraphAsync(segment);
    }

    public async Task TrainSegmentPairAsync(
        TranslationEngine engine,
        IReadOnlyList<string> sourceSegment,
        IReadOnlyList<string> targetSegment,
        bool sentenceStart
    )
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
        await runtime.TrainSegmentPairAsync(sourceSegment, targetSegment, sentenceStart);
    }

    public async Task<Build?> StartBuildAsync(TranslationEngine engine)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
        return await runtime.StartBuildAsync();
    }

    public async Task CancelBuildAsync(TranslationEngine engine)
    {
        CheckDisposed();

        if (_runtimes.TryGetValue(engine.Id, out ITranslationEngineRuntime? runtime))
            await runtime.CancelBuildAsync();
    }

    private ITranslationEngineRuntime GetOrCreateRuntime(TranslationEngine engine)
    {
        return _runtimes.GetOrAdd(engine.Id, _engineRuntimeFactories[engine.Type].CreateTranslationEngineRuntime);
    }

    private ITranslationEngineRuntime CreateRuntime(TranslationEngine engine)
    {
        ITranslationEngineRuntime runtime = _engineRuntimeFactories[engine.Type].CreateTranslationEngineRuntime(
            engine.Id
        );
        _runtimes.TryAdd(engine.Id, runtime);
        return runtime;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await _commitTimer.DisposeAsync();
        foreach (ITranslationEngineRuntime runtime in _runtimes.Values)
            await runtime.DisposeAsync();
        _runtimes.Clear();
    }
}
