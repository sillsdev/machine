namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IDataAccessContext dataAccessContext,
    IBuildJobService buildJobService,
    ILogger<SmtTransferBuildJob> logger,
    IRepository<TrainSegmentPair> trainSegmentPairs,
    ITruecaserFactory truecaserFactory,
    ISmtModelFactory smtModelFactory,
    ICorpusService corpusService
)
    : HangfireBuildJob<IReadOnlyList<Corpus>>(
        platformService,
        engines,
        lockFactory,
        dataAccessContext,
        buildJobService,
        logger
    )
{
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs = trainSegmentPairs;
    private readonly ITruecaserFactory _truecaserFactory = truecaserFactory;
    private readonly ISmtModelFactory _smtModelFactory = smtModelFactory;
    private readonly ICorpusService _corpusService = corpusService;

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
            ITextCorpus? sourceTextCorpus = _corpusService.CreateTextCorpora(corpus.SourceFiles).FirstOrDefault();
            ITextCorpus? targetTextCorpus = _corpusService.CreateTextCorpora(corpus.TargetFiles).FirstOrDefault();
            if (sourceTextCorpus is null || targetTextCorpus is null)
                continue;

            targetCorpora.Add(targetTextCorpus);
            parallelCorpora.Add(sourceTextCorpus.AlignRows(targetTextCorpus));

            if ((bool?)buildOptionsObject?["use_key_terms"] ?? true)
            {
                ITextCorpus? sourceTermCorpus = _corpusService.CreateTermCorpora(corpus.SourceFiles).FirstOrDefault();
                ITextCorpus? targetTermCorpus = _corpusService.CreateTermCorpora(corpus.TargetFiles).FirstOrDefault();
                if (sourceTermCorpus is not null && targetTermCorpus is not null)
                {
                    IParallelTextCorpus parallelKeyTermsCorpus = sourceTermCorpus.AlignRows(targetTermCorpus);
                    parallelCorpora.Add(parallelKeyTermsCorpus);
                }
            }
        }

        IParallelTextCorpus parallelCorpus = parallelCorpora.Flatten();
        ITextCorpus targetCorpus = targetCorpora.Flatten();

        var tokenizer = new LatinWordTokenizer();
        var detokenizer = new LatinWordDetokenizer();

        using ITrainer smtModelTrainer = await _smtModelFactory.CreateTrainerAsync(engineId, tokenizer, parallelCorpus);
        using ITrainer truecaseTrainer = await _truecaserFactory.CreateTrainerAsync(engineId, tokenizer, targetCorpus);

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
                IInteractiveTranslationModel smtModel = await _smtModelFactory.CreateAsync(
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

            await DataAccessContext.WithTransactionAsync(
                async (ct) =>
                {
                    await PlatformService.BuildCompletedAsync(
                        buildId,
                        smtModelTrainer.Stats.TrainCorpusSize + segmentPairs.Count,
                        smtModelTrainer.Stats.Metrics["bleu"] * 100.0,
                        CancellationToken.None
                    );
                    await BuildJobService.BuildJobFinishedAsync(
                        engineId,
                        buildId,
                        buildComplete: true,
                        CancellationToken.None
                    );
                },
                cancellationToken: CancellationToken.None
            );
        }

        stopwatch.Stop();
        Logger.LogInformation("Build completed in {0}s ({1})", stopwatch.Elapsed.TotalSeconds, buildId);
    }
}
