namespace SIL.Machine.AspNetCore.Services;

public abstract class TranslationEngineRuntimeBase<TJob> : AsyncDisposableBase, ITranslationEngineRuntime
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IDistributedReaderWriterLockFactory _lockFactory;

    protected TranslationEngineRuntimeBase(
        IBackgroundJobClient jobClient,
        IDistributedReaderWriterLockFactory lockFactory,
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        string engineId
    )
    {
        _jobClient = jobClient;
        _lockFactory = lockFactory;
        PlatformService = platformService;
        Lock = _lockFactory.Create(engineId);
        Engines = engines;
        EngineId = engineId;
        LastUsedTime = DateTime.UtcNow;
    }

    protected string EngineId { get; }
    protected IRepository<TranslationEngine> Engines { get; }
    protected IDistributedReaderWriterLock Lock { get; }
    protected IPlatformService PlatformService { get; }
    protected DateTime LastUsedTime { get; set; }

    public virtual async Task InitNewAsync()
    {
        CheckDisposed();

        await Engines.InsertAsync(new TranslationEngine { EngineId = EngineId });
    }

    public virtual Task<IReadOnlyList<(string Translation, TranslationResult Result)>> TranslateAsync(
        int n,
        string segment
    )
    {
        CheckDisposed();

        throw new NotSupportedException();
    }

    public virtual Task<WordGraph> GetWordGraphAsync(string segment)
    {
        CheckDisposed();

        throw new NotSupportedException();
    }

    public virtual Task TrainSegmentPairAsync(string sourceSegment, string targetSegment, bool sentenceStart)
    {
        CheckDisposed();

        throw new NotSupportedException();
    }

    public virtual async Task StartBuildAsync(string buildId)
    {
        CheckDisposed();

        // Use a lock to ensure that only one build is running at a time.
        await using (await Lock.WriterLockAsync())
        {
            // If there is a pending job, then no need to start a new one.
            if (await Engines.ExistsAsync(e => e.EngineId == EngineId && e.BuildState == BuildState.Pending))
                return;

            // cancel the existing build before starting a new build
            string? curBuildId = await CancelBuildInternalAsync();
            if (curBuildId is not null)
                await WaitForBuildToFinishAsync(curBuildId);

            // Schedule the job to occur way in the future, just so we can get the job id.
            string jobId = _jobClient.Schedule(GetJobExpression(buildId), TimeSpan.FromDays(1000));
            await Engines.UpdateAsync(
                e => e.EngineId == EngineId,
                u =>
                    u.Set(e => e.BuildState, BuildState.Pending)
                        .Set(e => e.IsCanceled, false)
                        .Set(e => e.JobId, jobId)
                        .Set(e => e.BuildId, buildId)
            );
            // Enqueue the job now that the build has been created.
            _jobClient.Requeue(jobId);
            LastUsedTime = DateTime.UtcNow;
        }
    }

    public virtual async Task CancelBuildAsync()
    {
        CheckDisposed();

        await using (await Lock.WriterLockAsync())
        {
            await CancelBuildInternalAsync();
            LastUsedTime = DateTime.UtcNow;
        }
    }

    public virtual Task CommitAsync()
    {
        CheckDisposed();

        return Task.CompletedTask;
    }

    public virtual async Task DeleteDataAsync()
    {
        CheckDisposed();

        await _lockFactory.DeleteAsync(EngineId);
    }

    protected abstract Expression<Func<TJob, Task>> GetJobExpression(string buildId);

    protected async Task<string?> CancelBuildInternalAsync()
    {
        // First, try to cancel a job that hasn't started yet
        TranslationEngine? engine = await Engines.UpdateAsync(
            e => e.EngineId == EngineId && e.BuildState == BuildState.Pending,
            u => u.Set(b => b.BuildState, BuildState.None).Set(e => e.IsCanceled, true)
        );
        if (engine is not null)
        {
            await PlatformService.BuildCanceledAsync(engine.BuildId!);
        }
        else
        {
            // Second, try to cancel a job that is already running
            engine = await Engines.UpdateAsync(
                e => e.EngineId == EngineId && e.BuildState == BuildState.Active,
                u => u.Set(b => b.IsCanceled, true)
            );
        }
        if (engine is not null)
        {
            // If pending, the job will be deleted from the queue, otherwise this will trigger the cancellation token
            _jobClient.Delete(engine.JobId);
        }
        return engine?.BuildId;
    }

    protected async Task WaitForBuildToFinishAsync(string buildId)
    {
        ISubscription<TranslationEngine> sub = await Engines.SubscribeAsync(
            e => e.EngineId == EngineId && e.BuildId == buildId
        );
        if (sub.Change.Entity is null)
            return;

        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < timeout)
        {
            await sub.WaitForChangeAsync(TimeSpan.FromSeconds(5));
            TranslationEngine? engine = sub.Change.Entity;
            if (engine is null || engine.BuildState is BuildState.None)
                return;
        }
    }
}
