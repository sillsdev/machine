namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineService : TranslationEngineServiceBase<SmtTransferEngineBuildJob>
{
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly SmtTransferEngineStateService _stateService;

    public SmtTransferEngineService(
        IBackgroundJobClient jobClient,
        IDistributedReaderWriterLockFactory lockFactory,
        IPlatformService platformService,
        IDataAccessContext dataAccessContext,
        IRepository<TranslationEngine> engines,
        IRepository<TrainSegmentPair> trainSegmentPairs,
        SmtTransferEngineStateService stateService
    )
        : base(jobClient, lockFactory, platformService, dataAccessContext, engines)
    {
        _trainSegmentPairs = trainSegmentPairs;
        _stateService = stateService;
    }

    public override TranslationEngineType Type => TranslationEngineType.SmtTransfer;

    public override async Task CreateAsync(
        string engineId,
        string? engineName,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    )
    {
        await base.CreateAsync(engineId, engineName, sourceLanguage, targetLanguage, cancellationToken);

        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = await LockFactory.CreateAsync(engineId, CancellationToken.None);
        await using (await @lock.WriterLockAsync(cancellationToken: CancellationToken.None))
        {
            state.InitNew();
        }
    }

    public override async Task DeleteAsync(string engineId, CancellationToken cancellationToken = default)
    {
        await base.DeleteAsync(engineId, cancellationToken);
        if (_stateService.TryRemove(engineId, out SmtTransferEngineState? state))
        {
            IDistributedReaderWriterLock @lock = await LockFactory.CreateAsync(engineId, CancellationToken.None);
            await using (await @lock.WriterLockAsync(cancellationToken: CancellationToken.None))
            {
                // ensure that there is no build running before unloading
                string? buildId = await CancelBuildInternalAsync(engineId, CancellationToken.None);
                if (buildId is not null)
                    await WaitForBuildToFinishAsync(engineId, buildId, CancellationToken.None);

                await state.DeleteDataAsync();
                await state.DisposeAsync();
            }
        }
    }

    public override async Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        string engineId,
        int n,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = await LockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.ReaderLockAsync(cancellationToken: cancellationToken))
        {
            TranslationEngine engine = await GetBuiltEngineAsync(engineId, cancellationToken);
            HybridTranslationEngine hybridEngine = await state.GetHybridEngineAsync(engine.BuildRevision);
            IReadOnlyList<TranslationResult> results = await hybridEngine.TranslateAsync(n, segment, cancellationToken);
            state.LastUsedTime = DateTime.Now;
            return results;
        }
    }

    public override async Task<WordGraph> GetWordGraphAsync(
        string engineId,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = await LockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.ReaderLockAsync(cancellationToken: cancellationToken))
        {
            TranslationEngine engine = await GetBuiltEngineAsync(engineId, cancellationToken);
            HybridTranslationEngine hybridEngine = await state.GetHybridEngineAsync(engine.BuildRevision);
            WordGraph result = await hybridEngine.GetWordGraphAsync(segment, cancellationToken);
            state.LastUsedTime = DateTime.Now;
            return result;
        }
    }

    public override async Task TrainSegmentPairAsync(
        string engineId,
        string sourceSegment,
        string targetSegment,
        bool sentenceStart,
        CancellationToken cancellationToken = default
    )
    {
        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = await LockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            TranslationEngine engine = await GetEngineAsync(engineId, cancellationToken);
            if (engine.BuildState is BuildState.Active)
            {
                await DataAccessContext.BeginTransactionAsync(cancellationToken);
                await _trainSegmentPairs.InsertAsync(
                    new TrainSegmentPair
                    {
                        TranslationEngineRef = engine.Id,
                        Source = sourceSegment,
                        Target = targetSegment,
                        SentenceStart = sentenceStart
                    },
                    cancellationToken
                );
            }

            HybridTranslationEngine hybridEngine = await state.GetHybridEngineAsync(engine.BuildRevision);
            await hybridEngine.TrainSegmentAsync(sourceSegment, targetSegment, sentenceStart, cancellationToken);
            await PlatformService.IncrementTrainSizeAsync(engineId, cancellationToken: CancellationToken.None);
            if (engine.BuildState is BuildState.Active)
                await DataAccessContext.CommitTransactionAsync(CancellationToken.None);
            state.IsUpdated = true;
            state.LastUsedTime = DateTime.Now;
        }
    }

    public override async Task StartBuildAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken = default
    )
    {
        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = await LockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await StartBuildInternalAsync(engineId, buildId, corpora, cancellationToken);
            state.LastUsedTime = DateTime.UtcNow;
        }
    }

    public override async Task CancelBuildAsync(string engineId, CancellationToken cancellationToken = default)
    {
        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = await LockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await CancelBuildInternalAsync(engineId, cancellationToken);
            state.LastUsedTime = DateTime.UtcNow;
        }
    }

    protected override Expression<Func<SmtTransferEngineBuildJob, Task>> GetJobExpression(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora
    )
    {
        // Token "None" is used here because hangfire injects the proper cancellation token
        return r => r.RunAsync(engineId, buildId, corpora, CancellationToken.None);
    }
}
