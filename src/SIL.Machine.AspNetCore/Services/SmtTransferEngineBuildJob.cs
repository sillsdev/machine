namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineBuildJob
{
    private readonly IPlatformService _platformService;
    private readonly IRepository<TranslationEngine> _engines;
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly IDistributedReaderWriterLockFactory _lockFactory;
    private readonly ITruecaserFactory _truecaserFactory;
    private readonly ISmtModelFactory _smtModelFactory;

    private readonly ILogger<SmtTransferEngineBuildJob> _logger;

    public SmtTransferEngineBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IRepository<TrainSegmentPair> trainSegmentPairs,
        IDistributedReaderWriterLockFactory lockFactory,
        ITruecaserFactory truecaserFactory,
        ISmtModelFactory smtModelFactory,
        ILogger<SmtTransferEngineBuildJob> logger
    )
    {
        _platformService = platformService;
        _engines = engines;
        _trainSegmentPairs = trainSegmentPairs;
        _lockFactory = lockFactory;
        _truecaserFactory = truecaserFactory;
        _smtModelFactory = smtModelFactory;
        _logger = logger;
    }

    [Queue("smt_transfer")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        IDistributedReaderWriterLock rwLock = _lockFactory.Create(engineId);

        ITrainer? smtModelTrainer = null;
        ITrainer? truecaseTrainer = null;
        try
        {
            var stopwatch = new Stopwatch();
            TranslationEngine? engine;
            await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                engine = await _engines.UpdateAsync(
                    e => e.EngineId == engineId && e.BuildId == buildId && !e.IsCanceled,
                    u => u.Set(e => e.BuildState, BuildState.Active),
                    cancellationToken: CancellationToken.None
                );
                if (engine is null)
                    throw new OperationCanceledException();

                await _platformService.BuildStartedAsync(buildId, cancellationToken);
                _logger.LogInformation("Build started ({0})", buildId);
                stopwatch.Start();

                await _trainSegmentPairs.DeleteAllAsync(p => p.TranslationEngineRef == engineId, cancellationToken);

                var targetCorpora = new List<ITextCorpus>();
                var parallelCorpora = new List<IParallelTextCorpus>();
                await foreach (CorpusInfo corpus in _platformService.GetCorporaAsync(engineId, cancellationToken))
                {
                    if (corpus.TargetCorpus is not null)
                        targetCorpora.Add(corpus.TargetCorpus);

                    if (corpus.SourceCorpus is not null && corpus.TargetCorpus is not null)
                        parallelCorpora.Add(corpus.SourceCorpus.AlignRows(corpus.TargetCorpus));
                }

                var tokenizer = new LatinWordTokenizer();
                IParallelTextCorpus parallelCorpus = parallelCorpora.Flatten().Tokenize(tokenizer).Lowercase();
                ITextCorpus targetCorpus = targetCorpora.Flatten().Tokenize(tokenizer);

                smtModelTrainer = _smtModelFactory.CreateTrainer(engineId, parallelCorpus);
                truecaseTrainer = _truecaserFactory.CreateTrainer(engineId, targetCorpus);
            }

            var progress = new BuildProgress(_platformService, buildId);
            await smtModelTrainer.TrainAsync(progress, cancellationToken);
            await truecaseTrainer.TrainAsync(cancellationToken: cancellationToken);
            int trainSegmentPairCount;
            await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await smtModelTrainer.SaveAsync();
                await truecaseTrainer.SaveAsync();
                ITruecaser truecaser = await _truecaserFactory.CreateAsync(engineId);
                IReadOnlyList<TrainSegmentPair> segmentPairs = await _trainSegmentPairs.GetAllAsync(
                    p => p.TranslationEngineRef == engine.Id,
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

                trainSegmentPairCount = segmentPairs.Count;

                await _engines.UpdateAsync(
                    e => e.EngineId == engineId && e.BuildId == buildId,
                    u =>
                        u.Set(e => e.BuildState, BuildState.None)
                            .Set(e => e.IsCanceled, false)
                            .Inc(e => e.BuildRevision)
                            .Unset(e => e.JobId)
                            .Unset(e => e.BuildId),
                    cancellationToken: CancellationToken.None
                );
            }

            await _platformService.BuildCompletedAsync(
                buildId,
                smtModelTrainer.Stats.TrainCorpusSize + trainSegmentPairCount,
                smtModelTrainer.Stats.Metrics["bleu"] * 100.0,
                CancellationToken.None
            );

            stopwatch.Stop();
            _logger.LogInformation("Build completed in {0}s ({1})", stopwatch.Elapsed.TotalSeconds, buildId);
        }
        catch (OperationCanceledException)
        {
            // Check if the cancellation was initiated by an API call or a shutdown.
            TranslationEngine? engine = await _engines.GetAsync(
                e => e.EngineId == engineId && e.BuildId == buildId,
                CancellationToken.None
            );
            if (engine is null || engine.IsCanceled)
            {
                await using (await rwLock.WriterLockAsync(cancellationToken: CancellationToken.None))
                {
                    await _engines.UpdateAsync(
                        e => e.EngineId == engineId && e.BuildId == buildId,
                        u =>
                            u.Set(e => e.BuildState, BuildState.None)
                                .Set(e => e.IsCanceled, false)
                                .Unset(e => e.JobId)
                                .Unset(e => e.BuildId),
                        cancellationToken: CancellationToken.None
                    );
                }
                await _platformService.BuildCanceledAsync(buildId, CancellationToken.None);
                _logger.LogInformation("Build canceled ({0})", buildId);
            }
            else if (engine is not null)
            {
                // the build was canceled, because of a server shutdown
                // switch state back to pending
                await using (await rwLock.WriterLockAsync(cancellationToken: CancellationToken.None))
                {
                    await _engines.UpdateAsync(
                        e => e.EngineId == engineId && e.BuildId == buildId && e.BuildState == BuildState.Active,
                        u => u.Set(e => e.BuildState, BuildState.Pending),
                        cancellationToken: CancellationToken.None
                    );
                }
                await _platformService.BuildRestartingAsync(buildId, CancellationToken.None);
            }

            throw;
        }
        catch (Exception e)
        {
            await using (await rwLock.WriterLockAsync(cancellationToken: CancellationToken.None))
            {
                await _engines.UpdateAsync(
                    e => e.EngineId == engineId && e.BuildId == buildId,
                    u =>
                        u.Set(e => e.BuildState, BuildState.None)
                            .Set(e => e.IsCanceled, false)
                            .Unset(e => e.JobId)
                            .Unset(e => e.BuildId),
                    cancellationToken: CancellationToken.None
                );
            }

            await _platformService.BuildFaultedAsync(buildId, e.Message, CancellationToken.None);
            _logger.LogError(0, e, "Build faulted ({0})", buildId);
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
