namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferBuildJob : HangfireBuildJob<IReadOnlyList<Corpus>>
{
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly ITruecaserFactory _truecaserFactory;
    private readonly ISmtModelFactory _smtModelFactory;
    private readonly ICorpusService _corpusService;

    public SmtTransferBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IDistributedReaderWriterLockFactory lockFactory,
        IBuildJobService buildJobService,
        ILogger<SmtTransferBuildJob> logger,
        IRepository<TrainSegmentPair> trainSegmentPairs,
        ITruecaserFactory truecaserFactory,
        ISmtModelFactory smtModelFactory,
        ICorpusService corpusService
    )
        : base(platformService, engines, lockFactory, buildJobService, logger)
    {
        _trainSegmentPairs = trainSegmentPairs;
        _truecaserFactory = truecaserFactory;
        _smtModelFactory = smtModelFactory;
        _corpusService = corpusService;
    }

    protected override Task InitializeAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> data,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        return _trainSegmentPairs.DeleteAllAsync(p => p.TranslationEngineRef == engineId, cancellationToken);
    }

    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        await PlatformService.BuildStartedAsync(buildId, cancellationToken);
        Logger.LogInformation("Build started ({0})", buildId);
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        cancellationToken.ThrowIfCancellationRequested();

        JsonObject? buildOptionsObject = null;
        if (buildOptions is not null)
        {
            buildOptionsObject = JsonSerializer.Deserialize<JsonObject>(buildOptions);
        }

        var targetCorpora = new List<ITextCorpus>();
        var parallelCorpora = new List<IParallelTextCorpus>();
        foreach (Corpus corpus in data)
        {
            IDictionary<CorpusType, ITextCorpus> scs = _corpusService.CreateTextCorpus(corpus.SourceFiles);
            IDictionary<CorpusType, ITextCorpus> tcs = _corpusService.CreateTextCorpus(corpus.TargetFiles);

            if (
                (bool?)buildOptionsObject?["use_key_terms"]
                ?? true && scs[CorpusType.Term] is not null && tcs[CorpusType.Term] is not null
            )
            {
                IParallelTextCorpus parallelKeyTermsCorpus = scs[CorpusType.Term].AlignRows(scs[CorpusType.Term]);
                parallelCorpora.Add(parallelKeyTermsCorpus);
            }

            targetCorpora.Add(tcs[CorpusType.Text]);
            parallelCorpora.Add(scs[CorpusType.Text].AlignRows(tcs[CorpusType.Text]));
        }

        IParallelTextCorpus parallelCorpus = parallelCorpora.Flatten();
        ITextCorpus targetCorpus = targetCorpora.Flatten();

        var tokenizer = new LatinWordTokenizer();
        var detokenizer = new LatinWordDetokenizer();

        using ITrainer smtModelTrainer = _smtModelFactory.CreateTrainer(engineId, tokenizer, parallelCorpus);
        using ITrainer truecaseTrainer = _truecaserFactory.CreateTrainer(engineId, tokenizer, targetCorpus);

        cancellationToken.ThrowIfCancellationRequested();

        var progress = new BuildProgress(PlatformService, buildId);
        await smtModelTrainer.TrainAsync(progress, cancellationToken);
        await truecaseTrainer.TrainAsync(cancellationToken: cancellationToken);

        TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
        if (engine is null)
            throw new OperationCanceledException();

        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await smtModelTrainer.SaveAsync(CancellationToken.None);
            await truecaseTrainer.SaveAsync(CancellationToken.None);
            ITruecaser truecaser = await _truecaserFactory.CreateAsync(engineId);
            IReadOnlyList<TrainSegmentPair> segmentPairs = await _trainSegmentPairs.GetAllAsync(
                p => p.TranslationEngineRef == engine.Id,
                CancellationToken.None
            );
            using (
                IInteractiveTranslationModel smtModel = _smtModelFactory.Create(
                    engineId,
                    tokenizer,
                    detokenizer,
                    truecaser
                )
            )
            {
                foreach (TrainSegmentPair segmentPair in segmentPairs)
                {
                    await smtModel.TrainSegmentAsync(
                        segmentPair.Source,
                        segmentPair.Target,
                        cancellationToken: CancellationToken.None
                    );
                }
            }

            await PlatformService.BuildCompletedAsync(
                buildId,
                smtModelTrainer.Stats.TrainCorpusSize + segmentPairs.Count,
                smtModelTrainer.Stats.Metrics["bleu"] * 100.0,
                CancellationToken.None
            );
            await BuildJobService.BuildJobFinishedAsync(engineId, buildId, buildComplete: true, CancellationToken.None);
        }

        stopwatch.Stop();
        Logger.LogInformation("Build completed in {0}s ({1})", stopwatch.Elapsed.TotalSeconds, buildId);
    }
}
