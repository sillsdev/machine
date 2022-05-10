namespace SIL.Machine.WebApi.Services;

public class TranslationEngineService : EntityServiceBase<TranslationEngine>, ITranslationEngineService
{
	private readonly IOptions<TranslationEngineOptions> _translationEngineOptions;
	private readonly ConcurrentDictionary<string, ITranslationEngineRuntime> _runtimes;
	private readonly IRepository<Build> _builds;
	private readonly Dictionary<TranslationEngineType, ITranslationEngineRuntimeFactory> _engineRuntimeFactories;
	private readonly AsyncTimer _commitTimer;

	public TranslationEngineService(IOptions<TranslationEngineOptions> translationEngineOptions,
		IRepository<TranslationEngine> translationEngines, IRepository<Build> builds,
		IEnumerable<ITranslationEngineRuntimeFactory> engineRuntimeFactories)
		: base(translationEngines)
	{
		_translationEngineOptions = translationEngineOptions;
		_builds = builds;
		_engineRuntimeFactories = engineRuntimeFactories.ToDictionary(f => f.Type);
		_runtimes = new ConcurrentDictionary<string, ITranslationEngineRuntime>();
		_commitTimer = new AsyncTimer(EngineCommitAsync);
	}

	public void Init()
	{
		_commitTimer.Start(_translationEngineOptions.Value.EngineCommitFrequency);
	}

	private async Task EngineCommitAsync()
	{
		foreach (ITranslationEngineRuntime runtime in _runtimes.Values)
			await runtime.CommitAsync();
	}

	public async Task<TranslationResult?> TranslateAsync(string engineId, IReadOnlyList<string> segment)
	{
		CheckDisposed();

		TranslationEngine? engine = await GetAsync(engineId);
		if (engine == null)
			return null;
		ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
		return await runtime.TranslateAsync(segment);
	}

	public async Task<IEnumerable<TranslationResult>?> TranslateAsync(string engineId, int n,
		IReadOnlyList<string> segment)
	{
		CheckDisposed();

		TranslationEngine? engine = await GetAsync(engineId);
		if (engine == null)
			return null;
		ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
		return await runtime.TranslateAsync(n, segment);
	}

	public async Task<WordGraph?> GetWordGraphAsync(string engineId, IReadOnlyList<string> segment)
	{
		CheckDisposed();

		TranslationEngine? engine = await GetAsync(engineId);
		if (engine == null)
			return null;
		ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
		return await runtime.GetWordGraphAsync(segment);
	}

	public async Task<bool> TrainSegmentAsync(string engineId, IReadOnlyList<string> sourceSegment,
		IReadOnlyList<string> targetSegment, bool sentenceStart)
	{
		CheckDisposed();

		TranslationEngine? engine = await GetAsync(engineId);
		if (engine == null)
			return false;
		ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
		await runtime.TrainSegmentPairAsync(sourceSegment, targetSegment, sentenceStart);
		return true;
	}

	public async Task<IEnumerable<TranslationEngine>> GetAllAsync(string owner)
	{
		CheckDisposed();

		return await Entities.GetAllAsync(e => e.Owner == owner);
	}

	public override async Task CreateAsync(TranslationEngine engine)
	{
		CheckDisposed();

		await Entities.InsertAsync(engine);
		ITranslationEngineRuntime runtime = CreateRuntime(engine);
		await runtime.InitNewAsync();
	}

	public override async Task<bool> DeleteAsync(string engineId)
	{
		CheckDisposed();

		TranslationEngine? engine = await Entities.DeleteAsync(engineId);
		if (engine == null)
			return false;
		await _builds.DeleteAllAsync(b => b.ParentRef == engineId);

		ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
		// the engine will have no associated projects, so remove it
		_runtimes.TryRemove(engineId, out _);
		await runtime.DeleteDataAsync();
		await runtime.DisposeAsync();
		return true;
	}

	public async Task<Build?> StartBuildAsync(string engineId)
	{
		CheckDisposed();

		TranslationEngine? engine = await GetAsync(engineId);
		if (engine == null)
			return null;
		ITranslationEngineRuntime runtime = GetOrCreateRuntime(engine);
		return await runtime.StartBuildAsync();
	}

	public async Task CancelBuildAsync(string engineId)
	{
		CheckDisposed();

		if (_runtimes.TryGetValue(engineId, out ITranslationEngineRuntime? runtime))
			await runtime.CancelBuildAsync();
	}

	public async Task<(TranslationEngine? Engine, ITranslationEngineRuntime? Runtime)> GetEngineAsync(string engineId)
	{
		CheckDisposed();

		TranslationEngine? engine = await GetAsync(engineId);
		if (engine == null)
			return (null, null);
		return (engine, GetOrCreateRuntime(engine));
	}

	public Task AddCorpusAsync(string engineId, TranslationEngineCorpus corpus)
	{
		CheckDisposed();

		return Entities.UpdateAsync(engineId, u => u.Add(e => e.Corpora, corpus));
	}

	public Task<bool> DeleteCorpusAsync(string engineId, string corpusId)
	{
		throw new NotImplementedException();
	}

	private ITranslationEngineRuntime GetOrCreateRuntime(TranslationEngine engine)
	{
		return _runtimes.GetOrAdd(engine.Id, _engineRuntimeFactories[engine.Type].CreateTranslationEngineRuntime);
	}

	private ITranslationEngineRuntime CreateRuntime(TranslationEngine engine)
	{
		ITranslationEngineRuntime runtime = _engineRuntimeFactories[engine.Type]
			.CreateTranslationEngineRuntime(engine.Id);
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
