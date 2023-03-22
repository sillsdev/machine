﻿namespace SIL.Machine.AspNetCore.Services;

public abstract class TranslationEngineServiceBase<TJob> : ITranslationEngineService
{
    private readonly IBackgroundJobClient _jobClient;

    protected TranslationEngineServiceBase(
        IBackgroundJobClient jobClient,
        IDistributedReaderWriterLockFactory lockFactory,
        IPlatformService platformService,
        IDataAccessContext dataAccessContext,
        IRepository<TranslationEngine> engines
    )
    {
        _jobClient = jobClient;
        LockFactory = lockFactory;
        PlatformService = platformService;
        DataAccessContext = dataAccessContext;
        Engines = engines;
    }

    protected IRepository<TranslationEngine> Engines { get; }
    protected IDistributedReaderWriterLockFactory LockFactory { get; }
    protected IPlatformService PlatformService { get; }
    protected IDataAccessContext DataAccessContext { get; }

    public abstract TranslationEngineType Type { get; }

    public virtual async Task CreateAsync(
        string engineId,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    )
    {
        await Engines.InsertAsync(
            new TranslationEngine
            {
                EngineId = engineId,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage
            },
            cancellationToken
        );
    }

    public virtual async Task DeleteAsync(string engineId, CancellationToken cancellationToken = default)
    {
        await DataAccessContext.BeginTransactionAsync(cancellationToken);
        await Engines.DeleteAsync(e => e.EngineId == engineId, cancellationToken);
        await LockFactory.DeleteAsync(engineId, cancellationToken);
        await DataAccessContext.CommitTransactionAsync(CancellationToken.None);
    }

    public virtual async Task StartBuildAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken = default
    )
    {
        IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await StartBuildInternalAsync(engineId, buildId, corpora, cancellationToken);
        }
    }

    public virtual async Task CancelBuildAsync(string engineId, CancellationToken cancellationToken = default)
    {
        IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
        await using (await @lock.WriterLockAsync())
        {
            await CancelBuildInternalAsync(engineId, cancellationToken);
        }
    }

    public virtual Task<IReadOnlyList<(string Translation, TranslationResult Result)>> TranslateAsync(
        string engineId,
        int n,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    public virtual Task<WordGraph> GetWordGraphAsync(
        string engineId,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    public virtual Task TrainSegmentPairAsync(
        string engineId,
        string sourceSegment,
        string targetSegment,
        bool sentenceStart,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException();
    }

    protected abstract Expression<Func<TJob, Task>> GetJobExpression(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora
    );

    protected async Task StartBuildInternalAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken
    )
    {
        // If there is a pending job, then no need to start a new one.
        if (
            await Engines.ExistsAsync(
                e => e.EngineId == engineId && e.BuildState == BuildState.Pending,
                cancellationToken
            )
        )
            return;

        // cancel the existing build before starting a new build
        string? curBuildId = await CancelBuildInternalAsync(engineId, cancellationToken);
        if (curBuildId is not null)
            await WaitForBuildToFinishAsync(engineId, curBuildId, CancellationToken.None);

        // Schedule the job to occur way in the future, just so we can get the job id.
        string jobId = _jobClient.Schedule(GetJobExpression(engineId, buildId, corpora), TimeSpan.FromDays(10000));
        await DataAccessContext.BeginTransactionAsync(CancellationToken.None);
        try
        {
            await Engines.UpdateAsync(
                e => e.EngineId == engineId,
                u =>
                    u.Set(e => e.BuildState, BuildState.Pending)
                        .Set(e => e.IsCanceled, false)
                        .Set(e => e.JobId, jobId)
                        .Set(e => e.BuildId, buildId),
                cancellationToken: CancellationToken.None
            );
            // Enqueue the job now that the build has been created.
            _jobClient.Requeue(jobId);
        }
        catch
        {
            await DataAccessContext.AbortTransactionAsync(CancellationToken.None);
            _jobClient.Delete(jobId);
            throw;
        }
        await DataAccessContext.CommitTransactionAsync(CancellationToken.None);
    }

    protected async Task<string?> CancelBuildInternalAsync(string engineId, CancellationToken cancellationToken)
    {
        await DataAccessContext.BeginTransactionAsync(cancellationToken);
        // First, try to cancel a job that hasn't started yet
        TranslationEngine? engine = await Engines.UpdateAsync(
            e => e.EngineId == engineId && e.BuildState == BuildState.Pending,
            u => u.Set(b => b.BuildState, BuildState.None).Set(e => e.IsCanceled, true),
            cancellationToken: cancellationToken
        );
        bool notifyPlatform = false;
        if (engine is not null)
        {
            notifyPlatform = true;
        }
        else
        {
            // Second, try to cancel a job that is already running
            engine = await Engines.UpdateAsync(
                e => e.EngineId == engineId && e.BuildState == BuildState.Active,
                u => u.Set(b => b.IsCanceled, true),
                cancellationToken: cancellationToken
            );
        }
        if (engine is not null)
        {
            // If pending, the job will be deleted from the queue, otherwise this will trigger the cancellation token
            _jobClient.Delete(engine.JobId);
            if (notifyPlatform)
                await PlatformService.BuildCanceledAsync(engine.BuildId!, CancellationToken.None);
        }
        await DataAccessContext.CommitTransactionAsync(CancellationToken.None);
        return engine?.BuildId;
    }

    protected async Task WaitForBuildToFinishAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        ISubscription<TranslationEngine> sub = await Engines.SubscribeAsync(
            e => e.EngineId == engineId && e.BuildId == buildId,
            cancellationToken
        );
        if (sub.Change.Entity is null)
            return;

        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < timeout)
        {
            await sub.WaitForChangeAsync(TimeSpan.FromSeconds(5), cancellationToken);
            TranslationEngine? engine = sub.Change.Entity;
            if (engine is null || engine.BuildState is BuildState.None)
                return;
        }
    }
}