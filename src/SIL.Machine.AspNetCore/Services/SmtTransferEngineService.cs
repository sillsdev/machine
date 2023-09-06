namespace SIL.Machine.AspNetCore.Services;

public static class SmtTransferBuildStages
{
    public const string Train = "train";
}

public class SmtTransferEngineService : ITranslationEngineService
{
    private readonly IDistributedReaderWriterLockFactory _lockFactory;
    private readonly IPlatformService _platformService;
    private readonly IDataAccessContext _dataAccessContext;
    private readonly IRepository<TranslationEngine> _engines;
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly SmtTransferEngineStateService _stateService;
    private readonly IBuildJobService _buildJobService;

    public SmtTransferEngineService(
        IDistributedReaderWriterLockFactory lockFactory,
        IPlatformService platformService,
        IDataAccessContext dataAccessContext,
        IRepository<TranslationEngine> engines,
        IRepository<TrainSegmentPair> trainSegmentPairs,
        SmtTransferEngineStateService stateService,
        IBuildJobService buildJobService
    )
    {
        _lockFactory = lockFactory;
        _platformService = platformService;
        _dataAccessContext = dataAccessContext;
        _engines = engines;
        _trainSegmentPairs = trainSegmentPairs;
        _stateService = stateService;
        _buildJobService = buildJobService;
    }

    public TranslationEngineType Type => TranslationEngineType.SmtTransfer;

    public async Task CreateAsync(
        string engineId,
        string? engineName,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    )
    {
        await _dataAccessContext.BeginTransactionAsync(cancellationToken);
        await _engines.InsertAsync(
            new TranslationEngine
            {
                EngineId = engineId,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage
            },
            cancellationToken
        );
        await _buildJobService.CreateEngineAsync(new[] { BuildJobType.Cpu }, engineId, engineName, cancellationToken);
        await _dataAccessContext.CommitTransactionAsync(CancellationToken.None);

        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, CancellationToken.None);
        await using (await @lock.WriterLockAsync(cancellationToken: CancellationToken.None))
        {
            SmtTransferEngineState state = _stateService.Get(engineId);
            state.InitNew();
        }
    }

    public async Task DeleteAsync(string engineId, CancellationToken cancellationToken = default)
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await CancelBuildJobAsync(engineId, cancellationToken);

            await _dataAccessContext.BeginTransactionAsync(cancellationToken);
            await _engines.DeleteAsync(e => e.EngineId == engineId, cancellationToken);
            await _trainSegmentPairs.DeleteAllAsync(p => p.TranslationEngineRef == engineId, cancellationToken);
            await _dataAccessContext.CommitTransactionAsync(CancellationToken.None);

            if (_stateService.TryRemove(engineId, out SmtTransferEngineState? state))
            {
                await state.DeleteDataAsync();
                await state.DisposeAsync();
            }
        }
        await _lockFactory.DeleteAsync(engineId, CancellationToken.None);
    }

    public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(
        string engineId,
        int n,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.ReaderLockAsync(cancellationToken: cancellationToken))
        {
            TranslationEngine engine = await GetBuiltEngineAsync(engineId, cancellationToken);
            SmtTransferEngineState state = _stateService.Get(engineId);
            HybridTranslationEngine hybridEngine = await state.GetHybridEngineAsync(engine.BuildRevision);
            IReadOnlyList<TranslationResult> results = await hybridEngine.TranslateAsync(n, segment, cancellationToken);
            state.LastUsedTime = DateTime.Now;
            return results;
        }
    }

    public async Task<WordGraph> GetWordGraphAsync(
        string engineId,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.ReaderLockAsync(cancellationToken: cancellationToken))
        {
            TranslationEngine engine = await GetBuiltEngineAsync(engineId, cancellationToken);
            SmtTransferEngineState state = _stateService.Get(engineId);
            HybridTranslationEngine hybridEngine = await state.GetHybridEngineAsync(engine.BuildRevision);
            WordGraph result = await hybridEngine.GetWordGraphAsync(segment, cancellationToken);
            state.LastUsedTime = DateTime.Now;
            return result;
        }
    }

    public async Task TrainSegmentPairAsync(
        string engineId,
        string sourceSegment,
        string targetSegment,
        bool sentenceStart,
        CancellationToken cancellationToken = default
    )
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            TranslationEngine engine = await GetEngineAsync(engineId, cancellationToken);
            if (engine.CurrentBuild?.JobState is BuildJobState.Active)
            {
                await _dataAccessContext.BeginTransactionAsync(cancellationToken);
                await _trainSegmentPairs.InsertAsync(
                    new TrainSegmentPair
                    {
                        TranslationEngineRef = engineId,
                        Source = sourceSegment,
                        Target = targetSegment,
                        SentenceStart = sentenceStart
                    },
                    cancellationToken
                );
            }

            SmtTransferEngineState state = _stateService.Get(engineId);
            HybridTranslationEngine hybridEngine = await state.GetHybridEngineAsync(engine.BuildRevision);
            await hybridEngine.TrainSegmentAsync(sourceSegment, targetSegment, sentenceStart, cancellationToken);
            await _platformService.IncrementTrainSizeAsync(engineId, cancellationToken: CancellationToken.None);
            if (engine.CurrentBuild?.JobState is BuildJobState.Active)
                await _dataAccessContext.CommitTransactionAsync(CancellationToken.None);
            state.IsUpdated = true;
            state.LastUsedTime = DateTime.Now;
        }
    }

    public async Task StartBuildAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken = default
    )
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            // If there is a pending/running build, then no need to start a new one.
            if (await _buildJobService.IsEngineBuilding(engineId, cancellationToken))
                throw new InvalidOperationException("The engine has already started a build.");

            await _buildJobService.StartBuildJobAsync(
                BuildJobType.Cpu,
                TranslationEngineType.SmtTransfer,
                engineId,
                buildId,
                SmtTransferBuildStages.Train,
                corpora,
                cancellationToken
            );
            SmtTransferEngineState state = _stateService.Get(engineId);
            state.LastUsedTime = DateTime.UtcNow;
        }
    }

    public async Task CancelBuildAsync(string engineId, CancellationToken cancellationToken = default)
    {
        IDistributedReaderWriterLock @lock = await _lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await CancelBuildJobAsync(engineId, cancellationToken);
            SmtTransferEngineState state = _stateService.Get(engineId);
            state.LastUsedTime = DateTime.UtcNow;
        }
    }

    private async Task CancelBuildJobAsync(string engineId, CancellationToken cancellationToken)
    {
        (string? buildId, BuildJobState jobState) = await _buildJobService.CancelBuildJobAsync(
            engineId,
            cancellationToken
        );
        if (buildId is not null && jobState is BuildJobState.None)
            await _platformService.BuildCanceledAsync(buildId, CancellationToken.None);
    }

    private async Task<TranslationEngine> GetEngineAsync(string engineId, CancellationToken cancellationToken)
    {
        TranslationEngine? engine = await _engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
        if (engine is null)
            throw new InvalidOperationException($"The engine {engineId} does not exist.");
        return engine;
    }

    private async Task<TranslationEngine> GetBuiltEngineAsync(string engineId, CancellationToken cancellationToken)
    {
        TranslationEngine engine = await GetEngineAsync(engineId, cancellationToken);
        if (engine.BuildRevision == 0)
            throw new EngineNotBuiltException("The engine must be built first.");
        return engine;
    }
}
