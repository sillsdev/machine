namespace SIL.Machine.WebApi.Services;

internal class EngineService : AsyncDisposableBase, IEngineService
{
	private readonly IOptions<EngineOptions> _engineOptions;
	private readonly ConcurrentDictionary<string, IEngineRuntime> _runtimes;
	private readonly IRepository<Engine> _engines;
	private readonly IRepository<Build> _builds;
	private readonly Dictionary<EngineType, IEngineRuntimeFactory> _engineRuntimeFactories;
	private readonly AsyncTimer _commitTimer;

	public EngineService(IOptions<EngineOptions> engineOptions, IRepository<Engine> engines,
		IRepository<Build> builds, IEnumerable<IEngineRuntimeFactory> engineRuntimeFactories)
	{
		_engineOptions = engineOptions;
		_engines = engines;
		_builds = builds;
		_engineRuntimeFactories = engineRuntimeFactories.ToDictionary(f => f.Type);
		_runtimes = new ConcurrentDictionary<string, IEngineRuntime>();
		_commitTimer = new AsyncTimer(EngineCommitAsync);
	}

	public void Init()
	{
		_commitTimer.Start(_engineOptions.Value.EngineCommitFrequency);
	}

	private async Task EngineCommitAsync()
	{
		foreach (IEngineRuntime runtime in _runtimes.Values)
			await runtime.CommitAsync();
	}

	public async Task<TranslationResult?> TranslateAsync(string engineId, IReadOnlyList<string> segment)
	{
		CheckDisposed();

		Engine? engine = await _engines.GetAsync(engineId);
		if (engine == null)
			return null;
		IEngineRuntime runtime = GetOrCreateRuntime(engine);
		return await runtime.TranslateAsync(segment);
	}

	public async Task<IEnumerable<TranslationResult>?> TranslateAsync(string engineId, int n,
		IReadOnlyList<string> segment)
	{
		CheckDisposed();

		Engine? engine = await _engines.GetAsync(engineId);
		if (engine == null)
			return null;
		IEngineRuntime runtime = GetOrCreateRuntime(engine);
		return await runtime.TranslateAsync(n, segment);
	}

	public async Task<WordGraph?> GetWordGraphAsync(string engineId, IReadOnlyList<string> segment)
	{
		CheckDisposed();

		Engine? engine = await _engines.GetAsync(engineId);
		if (engine == null)
			return null;
		IEngineRuntime runtime = GetOrCreateRuntime(engine);
		return await runtime.GetWordGraphAsync(segment);
	}

	public async Task<bool> TrainSegmentAsync(string engineId, IReadOnlyList<string> sourceSegment,
		IReadOnlyList<string> targetSegment, bool sentenceStart)
	{
		CheckDisposed();

		Engine? engine = await _engines.GetAsync(engineId);
		if (engine == null)
			return false;
		IEngineRuntime runtime = GetOrCreateRuntime(engine);
		await runtime.TrainSegmentPairAsync(sourceSegment, targetSegment, sentenceStart);
		return true;
	}

	public async Task<bool> CreateAsync(Engine engine)
	{
		CheckDisposed();

		try
		{
			await _engines.InsertAsync(engine);
			IEngineRuntime runtime = CreateRuntime(engine);
			await runtime.InitNewAsync();
		}
		catch (DuplicateKeyException)
		{
			// a project with the same id already exists
			return false;
		}
		return true;
	}

	public async Task<bool> DeleteAsync(string engineId)
	{
		CheckDisposed();

		Engine? engine = await _engines.DeleteAsync(engineId);
		if (engine == null)
			return false;
		await _builds.DeleteAllAsync(b => b.EngineRef == engineId);

		IEngineRuntime runtime = GetOrCreateRuntime(engine);
		// the engine will have no associated projects, so remove it
		_runtimes.TryRemove(engineId, out _);
		await runtime.DeleteDataAsync();
		await runtime.DisposeAsync();
		return true;
	}

	public async Task<Build?> StartBuildAsync(string engineId)
	{
		CheckDisposed();

		Engine? engine = await _engines.GetAsync(engineId);
		if (engine == null)
			return null;
		IEngineRuntime runtime = GetOrCreateRuntime(engine);
		return await runtime.StartBuildAsync();
	}

	public async Task CancelBuildAsync(string engineId)
	{
		CheckDisposed();

		if (_runtimes.TryGetValue(engineId, out IEngineRuntime? runtime))
			await runtime.CancelBuildAsync();
	}

	public async Task<(Engine? Engine, IEngineRuntime? Runtime)> GetEngineAsync(string engineId)
	{
		CheckDisposed();

		Engine? engine = await _engines.GetAsync(engineId);
		if (engine == null)
			return (null, null);
		return (engine, GetOrCreateRuntime(engine));
	}

	private IEngineRuntime GetOrCreateRuntime(Engine engine)
	{
		return _runtimes.GetOrAdd(engine.Id, _engineRuntimeFactories[engine.Type].CreateEngineRuntime);
	}

	private IEngineRuntime CreateRuntime(Engine engine)
	{
		IEngineRuntime runtime = _engineRuntimeFactories[engine.Type].CreateEngineRuntime(engine.Id);
		_runtimes.TryAdd(engine.Id, runtime);
		return runtime;
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		await _commitTimer.DisposeAsync();
		foreach (IEngineRuntime runtime in _runtimes.Values)
			await runtime.DisposeAsync();
		_runtimes.Clear();
	}
}
