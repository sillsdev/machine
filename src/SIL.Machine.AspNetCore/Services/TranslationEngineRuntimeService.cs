namespace SIL.Machine.AspNetCore.Services;

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
        _engineRuntimeFactories = engineRuntimeFactories
            .Where(f => _translationEngineOptions.CurrentValue.Types.Contains(f.Type))
            .ToDictionary(f => f.Type);
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

    public async Task CreateAsync(TranslationEngineType engineType, string engineId)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = CreateRuntime(engineType, engineId);
        await runtime.InitNewAsync();
    }

    public async Task DeleteAsync(TranslationEngineType engineType, string engineId)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engineType, engineId);
        _runtimes.TryRemove(engineId, out _);
        await runtime.DeleteDataAsync();
        await runtime.DisposeAsync();
    }

    public async Task<IEnumerable<(string Translation, TranslationResult Result)>> TranslateAsync(
        TranslationEngineType engineType,
        string engineId,
        int n,
        string segment
    )
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engineType, engineId);
        return await runtime.TranslateAsync(n, segment);
    }

    public async Task<WordGraph> GetWordGraphAsync(TranslationEngineType engineType, string engineId, string segment)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engineType, engineId);
        return await runtime.GetWordGraphAsync(segment);
    }

    public async Task TrainSegmentPairAsync(
        TranslationEngineType engineType,
        string engineId,
        string sourceSegment,
        string targetSegment,
        bool sentenceStart
    )
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engineType, engineId);
        await runtime.TrainSegmentPairAsync(sourceSegment, targetSegment, sentenceStart);
    }

    public async Task StartBuildAsync(TranslationEngineType engineType, string engineId, string buildId)
    {
        CheckDisposed();

        ITranslationEngineRuntime runtime = GetOrCreateRuntime(engineType, engineId);
        await runtime.StartBuildAsync(buildId);
    }

    public async Task CancelBuildAsync(string engineId)
    {
        CheckDisposed();

        if (_runtimes.TryGetValue(engineId, out ITranslationEngineRuntime? runtime))
            await runtime.CancelBuildAsync();
    }

    private ITranslationEngineRuntime GetOrCreateRuntime(TranslationEngineType engineType, string engineId)
    {
        return _runtimes.GetOrAdd(engineId, _engineRuntimeFactories[engineType].CreateTranslationEngineRuntime);
    }

    private ITranslationEngineRuntime CreateRuntime(TranslationEngineType engineType, string engineId)
    {
        ITranslationEngineRuntime runtime = _engineRuntimeFactories[engineType].CreateTranslationEngineRuntime(
            engineId
        );
        _runtimes.TryAdd(engineId, runtime);
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
