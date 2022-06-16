namespace SIL.Machine.WebApi.Services;

public class ClearMLNmtEngineRuntime : AsyncDisposableBase, ITranslationEngineRuntime
{
    public class Factory : TranslationEngineRuntimeFactory<ClearMLNmtEngineRuntime>
    {
        public Factory(IServiceProvider serviceProvider) : base(serviceProvider, TranslationEngineType.Nmt) { }
    }

    private readonly IRepository<TranslationEngine> _engines;
    private readonly IRepository<Build> _builds;
    private readonly IClearMLService _clearMLService;
    private readonly string _engineId;
    private readonly IBackgroundJobClient _jobClient;
    private readonly IDistributedReaderWriterLock _lock;
    private readonly IDistributedReaderWriterLockFactory _lockFactory;

    public ClearMLNmtEngineRuntime(
        IRepository<TranslationEngine> engines,
        IRepository<Build> builds,
        IClearMLService clearMLService,
        IBackgroundJobClient jobClient,
        IDistributedReaderWriterLockFactory lockFactory,
        string engineId
    )
    {
        _engines = engines;
        _builds = builds;
        _clearMLService = clearMLService;
        _jobClient = jobClient;
        _lockFactory = lockFactory;
        _lock = _lockFactory.Create(engineId);
        _engineId = engineId;
    }

    public async Task InitNewAsync()
    {
        CheckDisposed();

        TranslationEngine? engine = await _engines.GetAsync(_engineId);
        if (engine == null)
            return;
        await _clearMLService.CreateProjectAsync(engine.Id, engine.Name);
    }

    public Task<TranslationResult> TranslateAsync(IReadOnlyList<string> segment)
    {
        CheckDisposed();

        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment)
    {
        CheckDisposed();

        throw new NotSupportedException();
    }

    public Task<WordGraph> GetWordGraphAsync(IReadOnlyList<string> segment)
    {
        CheckDisposed();

        throw new NotSupportedException();
    }

    public Task TrainSegmentPairAsync(
        IReadOnlyList<string> sourceSegment,
        IReadOnlyList<string> targetSegment,
        bool sentenceStart
    )
    {
        CheckDisposed();

        throw new NotSupportedException();
    }

    public async Task<Build> StartBuildAsync()
    {
        CheckDisposed();

        // Use a lock to ensure that only one build is running at a time.
        await using (await _lock.WriterLockAsync())
        {
            // cancel the existing build before starting a new build
            string? buildId = await CancelBuildInternalAsync();
            if (buildId != null)
                await WaitForBuildToFinishAsync(buildId);

            buildId = ObjectId.GenerateNewId().ToString();
            // Schedule the job to occur way in the future, just so we can get the job id.
            string jobId = _jobClient.Schedule<ClearMLNmtEngineBuildJob>(
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
            return build;
        }
    }

    public async Task CancelBuildAsync()
    {
        CheckDisposed();

        await using (await _lock.WriterLockAsync())
            await CancelBuildInternalAsync();
    }

    public Task CommitAsync()
    {
        CheckDisposed();

        return Task.CompletedTask;
    }

    public async Task DeleteDataAsync()
    {
        CheckDisposed();

        string? projectId = await _clearMLService.GetProjectIdAsync(_engineId);
        if (projectId is null)
            return;

        await _clearMLService.DeleteProjectAsync(projectId);
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
}
