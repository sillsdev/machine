namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineRuntime : TranslationEngineRuntimeBase<SmtTransferEngineBuildJob>
{
    public class Factory : TranslationEngineRuntimeFactory<SmtTransferEngineRuntime>
    {
        public Factory(IServiceProvider serviceProvider) : base(serviceProvider, TranslationEngineType.SmtTransfer) { }
    }

    private readonly IPlatformService _platformService;
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly ISmtModelFactory _smtModelFactory;
    private readonly ITransferEngineFactory _transferEngineFactory;
    private readonly ITruecaserFactory _truecaserFactory;
    private readonly IOptionsMonitor<TranslationEngineOptions> _engineOptions;
    private readonly StringTokenizer _tokenizer;
    private readonly StringDetokenizer _detokenizer;

    private Lazy<IInteractiveTranslationModel> _smtModel;
    private Lazy<ITranslationEngine?> _transferEngine;
    private Lazy<HybridTranslationEngine> _hybridEngine;
    private AsyncLazy<ITruecaser> _truecaser;
    private bool _isUpdated;
    private int _currentBuildRevision = -1;

    public SmtTransferEngineRuntime(
        IOptionsMonitor<TranslationEngineOptions> engineOptions,
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IRepository<TrainSegmentPair> trainSegmentPairs,
        ISmtModelFactory smtModelFactory,
        ITransferEngineFactory transferEngineFactory,
        ITruecaserFactory truecaserFactory,
        IBackgroundJobClient jobClient,
        IDistributedReaderWriterLockFactory lockFactory,
        string engineId
    ) : base(jobClient, lockFactory, engines, engineId)
    {
        _platformService = platformService;
        _trainSegmentPairs = trainSegmentPairs;
        _smtModelFactory = smtModelFactory;
        _transferEngineFactory = transferEngineFactory;
        _truecaserFactory = truecaserFactory;
        _engineOptions = engineOptions;
        _smtModel = new Lazy<IInteractiveTranslationModel>(CreateSmtModel);
        _transferEngine = new Lazy<ITranslationEngine?>(CreateTransferEngine);
        _hybridEngine = new Lazy<HybridTranslationEngine>(CreateHybridEngine);
        _truecaser = new AsyncLazy<ITruecaser>(CreateTruecaserAsync);
        _tokenizer = new LatinWordTokenizer();
        _detokenizer = new LatinWordDetokenizer();
    }

    internal bool IsLoaded => _smtModel.IsValueCreated;

    public override async Task InitNewAsync()
    {
        CheckDisposed();

        await base.InitNewAsync();

        await using (await Lock.WriterLockAsync())
        {
            _smtModelFactory.InitNew(EngineId);
            _transferEngineFactory.InitNew(EngineId);
        }
    }

    public override async Task<IReadOnlyList<(string Translation, TranslationResult Result)>> TranslateAsync(
        int n,
        string segment
    )
    {
        CheckDisposed();

        IReadOnlyList<string> preprocSegment = _tokenizer.Tokenize(segment).ToArray().Lowercase();

        await using (await Lock.ReaderLockAsync())
        {
            await CheckReloadAsync();
            ITruecaser truecaser = await _truecaser;
            var results = new List<(string, TranslationResult)>();
            foreach (TranslationResult result in await _hybridEngine.Value.TranslateAsync(n, preprocSegment))
            {
                TranslationResult truecasedResult = truecaser.Truecase(result);
                results.Add((_detokenizer.Detokenize(truecasedResult.TargetSegment), truecasedResult));
            }
            LastUsedTime = DateTime.Now;
            return results;
        }
    }

    public override async Task<WordGraph> GetWordGraphAsync(string segment)
    {
        CheckDisposed();

        IReadOnlyList<string> preprocSegment = _tokenizer.Tokenize(segment).ToArray().Lowercase();

        await using (await Lock.ReaderLockAsync())
        {
            await CheckReloadAsync();
            WordGraph result = await _hybridEngine.Value.GetWordGraphAsync(preprocSegment);
            result = (await _truecaser).Truecase(result);
            LastUsedTime = DateTime.Now;
            return result;
        }
    }

    public override async Task TrainSegmentPairAsync(string sourceSegment, string targetSegment, bool sentenceStart)
    {
        CheckDisposed();

        List<string> tokenizedSourceSegment = _tokenizer.Tokenize(sourceSegment).ToList();
        IReadOnlyList<string> preprocSourceSegment = tokenizedSourceSegment.Lowercase();
        List<string> tokenizedTargetSegment = _tokenizer.Tokenize(targetSegment).ToList();
        IReadOnlyList<string> preprocTargetSegment = tokenizedTargetSegment.Lowercase();

        await using (await Lock.WriterLockAsync())
        {
            await CheckReloadAsync();
            await _hybridEngine.Value.TrainSegmentAsync(preprocSourceSegment, preprocTargetSegment);
            (await _truecaser).TrainSegment(tokenizedTargetSegment, sentenceStart);
            await _platformService.IncrementTrainSizeAsync(EngineId);
            TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == EngineId);
            if (engine is not null && engine.BuildState is BuildState.Active)
            {
                await _trainSegmentPairs.InsertAsync(
                    new TrainSegmentPair
                    {
                        TranslationEngineRef = engine.Id,
                        Source = tokenizedSourceSegment,
                        Target = tokenizedTargetSegment,
                        SentenceStart = sentenceStart
                    }
                );
            }
            _isUpdated = true;
            LastUsedTime = DateTime.Now;
        }
    }

    public override async Task CommitAsync()
    {
        CheckDisposed();

        await using (await Lock.WriterLockAsync())
        {
            if (!IsLoaded)
                return;

            TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == EngineId);
            if (engine is null || engine.BuildState is BuildState.Active)
                return;

            if (_currentBuildRevision == -1)
                _currentBuildRevision = engine.BuildRevision;
            if (engine.BuildRevision != _currentBuildRevision)
            {
                await UnloadAsync();
                _currentBuildRevision = engine.BuildRevision;
            }
            else if (DateTime.Now - LastUsedTime > _engineOptions.CurrentValue.InactiveEngineTimeout)
            {
                await UnloadAsync();
            }
            else
            {
                await SaveModelAsync();
            }
        }
    }

    public override async Task DeleteDataAsync()
    {
        CheckDisposed();

        await using (await Lock.WriterLockAsync())
        {
            // ensure that there is no build running before unloading
            string? buildId = await CancelBuildInternalAsync();
            if (buildId is not null)
                await WaitForBuildToFinishAsync(buildId);

            await UnloadAsync();
            _smtModelFactory.Cleanup(EngineId);
            _transferEngineFactory.Cleanup(EngineId);
            _truecaserFactory.Cleanup(EngineId);
        }

        await base.DeleteDataAsync();
    }

    private async Task SaveModelAsync()
    {
        if (_isUpdated)
        {
            await _smtModel.Value.SaveAsync();
            ITruecaser truecaser = await _truecaser;
            await truecaser.SaveAsync();
            _isUpdated = false;
        }
    }

    private async Task CheckReloadAsync()
    {
        if (!IsLoaded && _currentBuildRevision != -1)
            return;

        TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == EngineId);
        if (engine == null)
            return;

        if (_currentBuildRevision == -1)
            _currentBuildRevision = engine.BuildRevision;
        if (engine.BuildRevision != _currentBuildRevision)
        {
            _isUpdated = false;
            await UnloadAsync();
            _currentBuildRevision = engine.BuildRevision;
        }
    }

    private async Task UnloadAsync()
    {
        if (!IsLoaded)
            return;

        await SaveModelAsync();

        _smtModel.Value.Dispose();
        _transferEngine.Value?.Dispose();

        _smtModel = new Lazy<IInteractiveTranslationModel>(CreateSmtModel);
        _transferEngine = new Lazy<ITranslationEngine?>(CreateTransferEngine);
        _hybridEngine = new Lazy<HybridTranslationEngine>(CreateHybridEngine);
        _truecaser = new AsyncLazy<ITruecaser>(CreateTruecaserAsync);
        _currentBuildRevision = -1;
    }

    private IInteractiveTranslationModel CreateSmtModel()
    {
        return _smtModelFactory.Create(EngineId);
    }

    private ITranslationEngine? CreateTransferEngine()
    {
        return _transferEngineFactory.Create(EngineId);
    }

    private HybridTranslationEngine CreateHybridEngine()
    {
        IInteractiveTranslationEngine interactiveEngine = _smtModel.Value;
        ITranslationEngine? transferEngine = _transferEngine.Value;
        return new HybridTranslationEngine(interactiveEngine, transferEngine);
    }

    private Task<ITruecaser> CreateTruecaserAsync()
    {
        return _truecaserFactory.CreateAsync(EngineId)!;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await UnloadAsync();
    }

    protected override Expression<Func<SmtTransferEngineBuildJob, Task>> GetJobExpression(string buildId)
    {
        return r => r.RunAsync(EngineId, buildId, CancellationToken.None);
    }
}
