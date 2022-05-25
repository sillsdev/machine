namespace SIL.Machine.WebApi.Services;

public class NmtEngineRuntime : AsyncDisposableBase, ITranslationEngineRuntime
{
    public class Factory : TranslationEngineRuntimeFactory<NmtEngineRuntime>
    {
        public Factory(IServiceProvider serviceProvider) : base(serviceProvider, TranslationEngineType.Nmt) { }
    }

    private readonly IRepository<Build> _builds;
    private readonly string _engineId;
    private readonly IBackgroundJobClient _jobClient;
    private readonly IDistributedReaderWriterLock _lock;
    private readonly IDistributedReaderWriterLockFactory _lockFactory;

    public NmtEngineRuntime(
        IRepository<Build> builds,
        IBackgroundJobClient jobClient,
        IDistributedReaderWriterLockFactory lockFactory,
        string engineId
    )
    {
        _engineId = engineId;
        _builds = builds;
        _jobClient = jobClient;
        _lockFactory = lockFactory;
        _lock = _lockFactory.Create(_engineId);
    }

    public Task InitNewAsync()
    {
        CheckDisposed();

        return Task.CompletedTask;
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

        await using (await _lock.WriterLockAsync())
        {
            // cancel the existing build before starting a new build
            string? buildId = await CancelBuildInternalAsync();
            if (buildId != null)
                await WaitForBuildToFinishAsync(buildId);

            var build = new Build { ParentRef = _engineId };
            await _builds.InsertAsync(build);
            _jobClient.Enqueue<NmtEngineBuildJob>(
                r => r.RunAsync(_engineId, build.Id, default!, CancellationToken.None)
            );
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

    public Task DeleteDataAsync()
    {
        CheckDisposed();

        return Task.CompletedTask;
    }

    private async Task<string?> CancelBuildInternalAsync()
    {
        Build? build = await _builds.UpdateAsync(
            b => b.ParentRef == _engineId && (b.State == BuildState.Active || b.State == BuildState.Pending),
            u => u.Set(b => b.State, BuildState.Canceled)
        );
        if (build?.JobId != null)
            _jobClient.Delete(build.JobId);
        return build?.Id;
    }

    private async Task WaitForBuildToFinishAsync(string buildId)
    {
        ISubscription<Build> sub = await _builds.SubscribeAsync(b => b.Id == buildId);
        if (sub.Change.Entity == null)
            return;
        while (true)
        {
            await sub.WaitForChangeAsync(TimeSpan.FromSeconds(10));
            Build? build = sub.Change.Entity;
            if (build == null || build.DateFinished != null)
                return;
        }
    }
}
