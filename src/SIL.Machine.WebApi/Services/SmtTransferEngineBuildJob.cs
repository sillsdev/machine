namespace SIL.Machine.WebApi.Services;

public class SmtTransferEngineBuildJob
{
    private readonly IRepository<TranslationEngine> _engines;
    private readonly IRepository<Build> _builds;
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly IDistributedReaderWriterLockFactory _lockFactory;
    private readonly ICorpusService _corpusService;
    private readonly ITruecaserFactory _truecaserFactory;
    private readonly ISmtModelFactory _smtModelFactory;

    private readonly ILogger<SmtTransferEngineBuildJob> _logger;
    private readonly IWebhookService _webhookService;

    public SmtTransferEngineBuildJob(
        IRepository<TranslationEngine> engines,
        IRepository<Build> builds,
        IRepository<TrainSegmentPair> trainSegmentPairs,
        IDistributedReaderWriterLockFactory lockFactory,
        ICorpusService corpusService,
        ITruecaserFactory truecaserFactory,
        ISmtModelFactory smtModelFactory,
        ILogger<SmtTransferEngineBuildJob> logger,
        IWebhookService webhookService
    )
    {
        _engines = engines;
        _builds = builds;
        _trainSegmentPairs = trainSegmentPairs;
        _lockFactory = lockFactory;
        _corpusService = corpusService;
        _truecaserFactory = truecaserFactory;
        _smtModelFactory = smtModelFactory;
        _logger = logger;
        _webhookService = webhookService;
    }

    [Queue("smt_transfer")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        TranslationEngine? engine = await _engines.GetAsync(engineId, cancellationToken);
        if (engine is null)
            return;

        IDistributedReaderWriterLock rwLock = _lockFactory.Create(engineId);

        ITrainer? smtModelTrainer = null;
        ITrainer? truecaseTrainer = null;
        try
        {
            Build? build = await _builds.UpdateAsync(
                b => b.Id == buildId && b.State != BuildState.Canceled,
                u => u.Set(b => b.State, BuildState.Active),
                cancellationToken: cancellationToken
            );
            if (build is null)
                throw new OperationCanceledException();

            var stopwatch = new Stopwatch();
            await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                await _engines.UpdateAsync(
                    engineId,
                    u => u.Set(e => e.IsBuilding, true),
                    cancellationToken: CancellationToken.None
                );

                await _webhookService.SendEventAsync(WebhookEvent.BuildStarted, engine.Owner, build);

                _logger.LogInformation("Build started ({0})", buildId);
                stopwatch.Start();

                await _trainSegmentPairs.DeleteAllAsync(p => p.TranslationEngineRef == engineId, cancellationToken);

                var targetCorpora = new List<ITextCorpus>();
                var parallelCorpora = new List<IParallelTextCorpus>();
                foreach (TranslationEngineCorpus corpus in engine.Corpora)
                {
                    ITextCorpus? sc = await _corpusService.CreateTextCorpusAsync(
                        corpus.CorpusRef,
                        engine.SourceLanguageTag
                    );
                    ITextCorpus? tc = await _corpusService.CreateTextCorpusAsync(
                        corpus.CorpusRef,
                        engine.TargetLanguageTag
                    );
                    if (tc is not null)
                        targetCorpora.Add(tc);

                    if (sc is not null && tc is not null)
                        parallelCorpora.Add(sc.AlignRows(tc));
                }

                var tokenizer = new LatinWordTokenizer();
                IParallelTextCorpus parallelCorpus = parallelCorpora.Flatten().Tokenize(tokenizer).Lowercase();
                ITextCorpus targetCorpus = targetCorpora.Flatten().Tokenize(tokenizer);

                smtModelTrainer = _smtModelFactory.CreateTrainer(engineId, parallelCorpus);
                truecaseTrainer = _truecaserFactory.CreateTrainer(engineId, targetCorpus);
            }

            var progress = new BuildProgress(_builds, buildId);
            await smtModelTrainer.TrainAsync(progress, cancellationToken);
            await truecaseTrainer.TrainAsync(cancellationToken: cancellationToken);

            await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await smtModelTrainer.SaveAsync();
                await truecaseTrainer.SaveAsync();
                ITruecaser truecaser = await _truecaserFactory.CreateAsync(engineId);
                IReadOnlyList<TrainSegmentPair> segmentPairs = await _trainSegmentPairs.GetAllAsync(
                    p => p.TranslationEngineRef == engineId,
                    CancellationToken.None
                );
                using (IInteractiveTranslationModel smtModel = _smtModelFactory.Create(engineId))
                {
                    foreach (TrainSegmentPair segmentPair in segmentPairs)
                    {
                        await smtModel.TrainSegmentAsync(
                            segmentPair.Source.Lowercase(),
                            segmentPair.Target.Lowercase()
                        );
                        truecaser.TrainSegment(segmentPair.Target, segmentPair.SentenceStart);
                    }
                }

                await _engines.UpdateAsync(
                    engineId,
                    u =>
                        u.Set(
                                e => e.Confidence,
                                Math.Round(
                                    smtModelTrainer.Stats.Metrics["bleu"] * 100.0,
                                    2,
                                    MidpointRounding.AwayFromZero
                                )
                            )
                            .Set(e => e.CorpusSize, smtModelTrainer.Stats.TrainCorpusSize + segmentPairs.Count)
                            .Set(e => e.IsBuilding, false)
                            .Inc(e => e.ModelRevision),
                    cancellationToken: CancellationToken.None
                );
            }

            build = await _builds.UpdateAsync(
                buildId,
                u =>
                    u.Set(b => b.State, BuildState.Completed)
                        .Set(b => b.Message, "Completed")
                        .Set(b => b.DateFinished, DateTime.UtcNow),
                cancellationToken: CancellationToken.None
            );

            stopwatch.Stop();
            _logger.LogInformation("Build completed in {0}s ({1})", stopwatch.Elapsed.TotalSeconds, buildId);
            await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
        }
        catch (OperationCanceledException)
        {
            await using (await rwLock.WriterLockAsync(cancellationToken: CancellationToken.None))
            {
                await _engines.UpdateAsync(
                    e => e.Id == engineId && e.IsBuilding,
                    u => u.Set(e => e.IsBuilding, false),
                    cancellationToken: CancellationToken.None
                );
            }

            // Check if the cancellation was initiated by an API call or a shutdown.
            Build? build = await _builds.GetAsync(buildId, CancellationToken.None);
            if (build?.State is BuildState.Canceled)
            {
                build =
                    await _builds.UpdateAsync(
                        b => b.Id == buildId && b.DateFinished == null,
                        u => u.Set(b => b.DateFinished, DateTime.UtcNow),
                        cancellationToken: CancellationToken.None
                    ) ?? build;

                _logger.LogInformation("Build canceled ({0})", buildId);
                await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
            }
            else if (build is not null)
            {
                // the build was canceled, because of a server shutdown
                // switch state back to pending
                await _builds.UpdateAsync(
                    buildId,
                    u =>
                        u.Set(b => b.Message, "Restarting")
                            .Set(b => b.Step, 0)
                            .Set(b => b.PercentCompleted, 0)
                            .Set(b => b.State, BuildState.Pending),
                    cancellationToken: CancellationToken.None
                );
            }

            throw;
        }
        catch (Exception e)
        {
            await using (await rwLock.WriterLockAsync(cancellationToken: CancellationToken.None))
            {
                await _engines.UpdateAsync(
                    e => e.Id == engineId && e.IsBuilding,
                    u => u.Set(e => e.IsBuilding, false),
                    cancellationToken: CancellationToken.None
                );
            }

            Build? build = await _builds.UpdateAsync(
                buildId,
                u =>
                    u.Set(b => b.State, BuildState.Faulted)
                        .Set(b => b.Message, e.Message)
                        .Set(b => b.DateFinished, DateTime.UtcNow),
                cancellationToken: CancellationToken.None
            );
            if (build is null)
                return;
            _logger.LogError(0, e, "Build faulted ({0})", buildId);
            await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
            throw;
        }
        finally
        {
            smtModelTrainer?.Dispose();
            truecaseTrainer?.Dispose();
        }
    }

    private static IParallelTextCorpus CreateParallelCorpus(
        IReadOnlyDictionary<string, ITextCorpus> sourceCorpora,
        IReadOnlyDictionary<string, ITextCorpus> targetCorpora
    )
    {
        var parallelCorpora = new List<IParallelTextCorpus>();
        foreach (KeyValuePair<string, ITextCorpus> kvp in sourceCorpora)
        {
            if (targetCorpora.TryGetValue(kvp.Key, out ITextCorpus? targetCorpus))
            {
                ITextCorpus sourceCorpus = kvp.Value;
                parallelCorpora.Add(sourceCorpus.AlignRows(targetCorpus));
            }
        }
        return parallelCorpora.Flatten();
    }
}
