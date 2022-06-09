namespace SIL.Machine.WebApi.Services;

public class SmtTransferEngineRuntime : AsyncDisposableBase, ITranslationEngineRuntime
{
    public class Factory : TranslationEngineRuntimeFactory<SmtTransferEngineRuntime>
    {
        public Factory(IServiceProvider serviceProvider) : base(serviceProvider, TranslationEngineType.SmtTransfer) { }
    }

    private const int MaxEnginePoolSize = 3;

    private readonly IRepository<TranslationEngine> _engines;
    private readonly IRepository<Build> _builds;
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly ISmtModelFactory _smtModelFactory;
    private readonly ITransferEngineFactory _transferEngineFactory;
    private readonly ITruecaserFactory _truecaserFactory;
    private readonly IOptionsMonitor<TranslationEngineOptions> _engineOptions;
    private readonly IBackgroundJobClient _jobClient;
    private readonly string _engineId;
    private readonly IDistributedReaderWriterLock _lock;
    private readonly IDistributedReaderWriterLockFactory _lockFactory;

    private Lazy<IInteractiveTranslationModel> _smtModel;
    private ObjectPool<HybridTranslationEngine> _enginePool;
    private AsyncLazy<ITruecaser> _truecaser;
    private bool _isUpdated;
    private DateTime _lastUsedTime;
    private int _currentModelRevision = -1;

    public SmtTransferEngineRuntime(
        IOptionsMonitor<TranslationEngineOptions> engineOptions,
        IRepository<TranslationEngine> engines,
        IRepository<Build> builds,
        IRepository<TrainSegmentPair> trainSegmentPairs,
        ISmtModelFactory smtModelFactory,
        ITransferEngineFactory transferEngineFactory,
        ITruecaserFactory truecaserFactory,
        IBackgroundJobClient jobClient,
        IDistributedReaderWriterLockFactory lockFactory,
        string engineId
    )
    {
        _engines = engines;
        _builds = builds;
        _trainSegmentPairs = trainSegmentPairs;
        _smtModelFactory = smtModelFactory;
        _transferEngineFactory = transferEngineFactory;
        _truecaserFactory = truecaserFactory;
        _engineOptions = engineOptions;
        _jobClient = jobClient;
        _lockFactory = lockFactory;
        _engineId = engineId;
        _lock = _lockFactory.Create(_engineId);
        _smtModel = new Lazy<IInteractiveTranslationModel>(CreateSmtModel);
        _enginePool = new ObjectPool<HybridTranslationEngine>(MaxEnginePoolSize, CreateEngine);
        _truecaser = new AsyncLazy<ITruecaser>(CreateTruecaserAsync);
        _lastUsedTime = DateTime.Now;
    }

    internal bool IsLoaded => _smtModel.IsValueCreated;

    public async Task InitNewAsync()
    {
        CheckDisposed();

        await using (await _lock.WriterLockAsync())
        {
            _smtModelFactory.InitNew(_engineId);
            _transferEngineFactory.InitNew(_engineId);
        }
    }

    public async Task<TranslationResult> TranslateAsync(IReadOnlyList<string> segment)
    {
        CheckDisposed();

        IReadOnlyList<string> preprocSegment = segment.Lowercase();

        await using (await _lock.ReaderLockAsync())
        {
            await CheckReloadAsync();
            using ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync();
            TranslationResult result = item.Object.Translate(preprocSegment);
            result = (await _truecaser).Truecase(result);
            _lastUsedTime = DateTime.Now;
            return result;
        }
    }

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment)
    {
        CheckDisposed();

        IReadOnlyList<string> preprocSegment = segment.Lowercase();

        await using (await _lock.ReaderLockAsync())
        {
            await CheckReloadAsync();
            using ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync();
            ITruecaser truecaser = await _truecaser;
            var results = new List<TranslationResult>();
            foreach (TranslationResult result in item.Object.Translate(n, preprocSegment))
                results.Add(truecaser.Truecase(result));
            _lastUsedTime = DateTime.Now;
            return results;
        }
    }

    public async Task<WordGraph> GetWordGraphAsync(IReadOnlyList<string> segment)
    {
        CheckDisposed();

        IReadOnlyList<string> preprocSegment = segment.Lowercase();

        await using (await _lock.ReaderLockAsync())
        {
            await CheckReloadAsync();
            using ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync();
            WordGraph result = item.Object.GetWordGraph(preprocSegment);
            result = (await _truecaser).Truecase(result);
            _lastUsedTime = DateTime.Now;
            return result;
        }
    }

    public async Task TrainSegmentPairAsync(
        IReadOnlyList<string> sourceSegment,
        IReadOnlyList<string> targetSegment,
        bool sentenceStart
    )
    {
        CheckDisposed();

        IReadOnlyList<string> preprocSourceSegment = sourceSegment.Lowercase();
        IReadOnlyList<string> preprocTargetSegment = targetSegment.Lowercase();

        await using (await _lock.WriterLockAsync())
        {
            await CheckReloadAsync();
            using ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync();
            item.Object.TrainSegment(preprocSourceSegment, preprocTargetSegment);
            (await _truecaser).TrainSegment(targetSegment, sentenceStart);
            TranslationEngine? engine = await _engines.UpdateAsync(_engineId, u => u.Inc(e => e.CorpusSize, 1));
            if (engine is not null && engine.IsBuilding)
            {
                await _trainSegmentPairs.InsertAsync(
                    new TrainSegmentPair
                    {
                        TranslationEngineRef = _engineId,
                        Source = sourceSegment.ToList(),
                        Target = targetSegment.ToList(),
                        SentenceStart = sentenceStart
                    }
                );
            }
            _isUpdated = true;
            _lastUsedTime = DateTime.Now;
        }
    }

    public async Task<Build> StartBuildAsync()
    {
        CheckDisposed();

        await using (await _lock.WriterLockAsync())
        {
            // cancel the existing build before starting a new build
            string? buildId = await CancelBuildInternalAsync();
            if (buildId is not null)
                await WaitForBuildToFinishAsync(buildId);

            buildId = ObjectId.GenerateNewId().ToString();
            // Schedule the job to occur way in the future, just so we can get the job id.
            string jobId = _jobClient.Schedule<SmtTransferEngineBuildJob>(
                r => r.RunAsync(_engineId, buildId, CancellationToken.None),
                TimeSpan.FromDays(1000)
            );
            var build = new Build
            {
                Id = buildId,
                ParentRef = _engineId,
                JobId = jobId
            };
            await _builds.InsertAsync(build);
            // Enqueue the job now that the build has been created.
            _jobClient.Requeue(jobId);
            _lastUsedTime = DateTime.Now;
            return build;
        }
    }

    public async Task CancelBuildAsync()
    {
        CheckDisposed();

        await using (await _lock.WriterLockAsync())
            await CancelBuildInternalAsync();
    }

    public async Task CommitAsync()
    {
        CheckDisposed();

        await using (await _lock.WriterLockAsync())
        {
            if (!IsLoaded)
                return;

            TranslationEngine? engine = await _engines.GetAsync(_engineId);
            if (engine is null || engine.IsBuilding)
                return;

            if (_currentModelRevision == -1)
                _currentModelRevision = engine.ModelRevision;
            if (engine.ModelRevision != _currentModelRevision)
            {
                await UnloadAsync();
                _currentModelRevision = engine.ModelRevision;
            }
            else if (DateTime.Now - _lastUsedTime > _engineOptions.CurrentValue.InactiveEngineTimeout)
            {
                await UnloadAsync();
            }
            else
            {
                await SaveModelAsync();
            }
        }
    }

    public async Task DeleteDataAsync()
    {
        CheckDisposed();

        await using (await _lock.WriterLockAsync())
        {
            // ensure that there is no build running before unloading
            string? buildId = await CancelBuildInternalAsync();
            if (buildId is not null)
                await WaitForBuildToFinishAsync(buildId);

            await UnloadAsync();
            _smtModelFactory.Cleanup(_engineId);
            _transferEngineFactory.Cleanup(_engineId);
            _truecaserFactory.Cleanup(_engineId);
        }
        await _lockFactory.DeleteAsync(_engineId);
    }

    private async Task<string?> CancelBuildInternalAsync()
    {
        // First, try to cancel a job that hasn't started yet
        Build? build = await _builds.UpdateAsync(
            b => b.ParentRef == _engineId && b.State == BuildState.Pending,
            u => u.Set(b => b.State, BuildState.Canceled).Set(b => b.DateFinished, DateTime.UtcNow)
        );
        if (build is null)
        {
            // Second, try to cancel a job that is already running
            build = await _builds.UpdateAsync(
                b => b.ParentRef == _engineId && b.State == BuildState.Active,
                u => u.Set(b => b.State, BuildState.Canceled)
            );
        }
        if (build is not null)
            // If pending, the job will be deleted from the queue, otherwise this will trigger the cancellation token
            _jobClient.Delete(build.JobId);
        return build?.Id;
    }

    private async Task WaitForBuildToFinishAsync(string buildId)
    {
        ISubscription<Build> sub = await _builds.SubscribeAsync(b => b.Id == buildId);
        if (sub.Change.Entity is null)
            return;

        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < timeout)
        {
            await sub.WaitForChangeAsync(TimeSpan.FromSeconds(5));
            Build? build = sub.Change.Entity;
            if (build is null || build.DateFinished is not null)
                return;
        }
    }

    private async Task SaveModelAsync()
    {
        if (_isUpdated)
        {
            _smtModel.Value.Save();
            ITruecaser truecaser = await _truecaser;
            await truecaser.SaveAsync();
            _isUpdated = false;
        }
    }

    private async Task CheckReloadAsync()
    {
        if (!IsLoaded && _currentModelRevision != -1)
            return;

        TranslationEngine? engine = await _engines.GetAsync(_engineId);
        if (engine == null)
            return;

        if (_currentModelRevision == -1)
            _currentModelRevision = engine.ModelRevision;
        if (engine.ModelRevision != _currentModelRevision)
        {
            _isUpdated = false;
            await UnloadAsync();
            _currentModelRevision = engine.ModelRevision;
        }
    }

    private async Task UnloadAsync()
    {
        if (!IsLoaded)
            return;

        await SaveModelAsync();

        _enginePool.Dispose();
        _smtModel.Value.Dispose();

        _smtModel = new Lazy<IInteractiveTranslationModel>(CreateSmtModel);
        _enginePool = new ObjectPool<HybridTranslationEngine>(MaxEnginePoolSize, CreateEngine);
        _truecaser = new AsyncLazy<ITruecaser>(CreateTruecaserAsync);
        _currentModelRevision = -1;
    }

    private IInteractiveTranslationModel CreateSmtModel()
    {
        return _smtModelFactory.Create(_engineId);
    }

    private HybridTranslationEngine CreateEngine()
    {
        IInteractiveTranslationEngine interactiveEngine = _smtModel.Value.CreateInteractiveEngine();
        ITranslationEngine? ruleEngine = _transferEngineFactory.Create(_engineId);
        return new HybridTranslationEngine(interactiveEngine, ruleEngine);
    }

    private Task<ITruecaser> CreateTruecaserAsync()
    {
        return _truecaserFactory.CreateAsync(_engineId)!;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await UnloadAsync();
    }
}
