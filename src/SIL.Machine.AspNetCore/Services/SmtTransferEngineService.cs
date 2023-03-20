namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineService : TranslationEngineServiceBase<SmtTransferEngineBuildJob>
{
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly SmtTransferEngineStateService _stateService;

    private readonly StringTokenizer _tokenizer;
    private readonly StringDetokenizer _detokenizer;

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

        _tokenizer = new LatinWordTokenizer();
        _detokenizer = new LatinWordDetokenizer();
    }

    public override TranslationEngineType Type => TranslationEngineType.SmtTransfer;

    public override async Task CreateAsync(
        string engineId,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default
    )
    {
        await base.CreateAsync(engineId, sourceLanguage, targetLanguage, cancellationToken);

        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
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
            IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
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

    public override async Task<IReadOnlyList<(string Translation, TranslationResult Result)>> TranslateAsync(
        string engineId,
        int n,
        string segment,
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<string> preprocSegment = _tokenizer.Tokenize(segment).ToArray().Lowercase();

        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
        await using (await @lock.ReaderLockAsync(cancellationToken: cancellationToken))
        {
            await CheckReloadAsync(state, cancellationToken);
            ITruecaser truecaser = await state.Truecaser;
            var results = new List<(string, TranslationResult)>();
            foreach (
                TranslationResult result in await state.HybridEngine.Value.TranslateAsync(
                    n,
                    preprocSegment,
                    cancellationToken
                )
            )
            {
                TranslationResult truecasedResult = truecaser.Truecase(result);
                results.Add((_detokenizer.Detokenize(truecasedResult.TargetSegment), truecasedResult));
            }
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
        IReadOnlyList<string> preprocSegment = _tokenizer.Tokenize(segment).ToArray().Lowercase();

        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
        await using (await @lock.ReaderLockAsync(cancellationToken: cancellationToken))
        {
            await CheckReloadAsync(state, cancellationToken);
            WordGraph result = await state.HybridEngine.Value.GetWordGraphAsync(preprocSegment, cancellationToken);
            result = (await state.Truecaser).Truecase(result);
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
        List<string> tokenizedSourceSegment = _tokenizer.Tokenize(sourceSegment).ToList();
        IReadOnlyList<string> preprocSourceSegment = tokenizedSourceSegment.Lowercase();
        List<string> tokenizedTargetSegment = _tokenizer.Tokenize(targetSegment).ToList();
        IReadOnlyList<string> preprocTargetSegment = tokenizedTargetSegment.Lowercase();

        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await CheckReloadAsync(state, cancellationToken);
            TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
            if (engine is not null && engine.BuildState is BuildState.Active)
            {
                await DataAccessContext.BeginTransactionAsync(cancellationToken);
                await _trainSegmentPairs.InsertAsync(
                    new TrainSegmentPair
                    {
                        TranslationEngineRef = engine.Id,
                        Source = tokenizedSourceSegment,
                        Target = tokenizedTargetSegment,
                        SentenceStart = sentenceStart
                    },
                    cancellationToken
                );
            }

            await state.HybridEngine.Value.TrainSegmentAsync(
                preprocSourceSegment,
                preprocTargetSegment,
                cancellationToken: cancellationToken
            );
            (await state.Truecaser).TrainSegment(tokenizedTargetSegment, sentenceStart);
            await PlatformService.IncrementTrainSizeAsync(engineId, cancellationToken: CancellationToken.None);
            if (engine is not null && engine.BuildState is BuildState.Active)
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
        IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            await StartBuildInternalAsync(engineId, buildId, corpora, cancellationToken);
            state.LastUsedTime = DateTime.UtcNow;
        }
    }

    public override async Task CancelBuildAsync(string engineId, CancellationToken cancellationToken = default)
    {
        SmtTransferEngineState state = _stateService.Get(engineId);
        IDistributedReaderWriterLock @lock = LockFactory.Create(engineId);
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
        return r => r.RunAsync(engineId, buildId, corpora, CancellationToken.None);
    }

    private async Task CheckReloadAsync(SmtTransferEngineState state, CancellationToken cancellationToken)
    {
        if (!state.IsLoaded && state.CurrentBuildRevision != -1)
            return;

        TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == state.EngineId, cancellationToken);
        if (engine == null)
            return;

        if (state.CurrentBuildRevision == -1)
            state.CurrentBuildRevision = engine.BuildRevision;
        if (engine.BuildRevision != state.CurrentBuildRevision)
        {
            state.IsUpdated = false;
            await state.UnloadAsync();
            state.CurrentBuildRevision = engine.BuildRevision;
        }
    }
}
