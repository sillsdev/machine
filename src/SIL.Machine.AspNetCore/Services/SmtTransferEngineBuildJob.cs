namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferEngineBuildJob
{
    private readonly IPlatformService _platformService;
    private readonly IRepository<TranslationEngine> _engines;
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
    private readonly IDistributedReaderWriterLockFactory _lockFactory;
    private readonly ITruecaserFactory _truecaserFactory;
    private readonly ISmtModelFactory _smtModelFactory;
    private readonly ICorpusService _corpusService;

    private readonly ILogger<SmtTransferEngineBuildJob> _logger;

    public SmtTransferEngineBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IRepository<TrainSegmentPair> trainSegmentPairs,
        IDistributedReaderWriterLockFactory lockFactory,
        ITruecaserFactory truecaserFactory,
        ISmtModelFactory smtModelFactory,
        ICorpusService corpusService,
        ILogger<SmtTransferEngineBuildJob> logger
    )
    {
        _platformService = platformService;
        _engines = engines;
        _trainSegmentPairs = trainSegmentPairs;
        _lockFactory = lockFactory;
        _truecaserFactory = truecaserFactory;
        _smtModelFactory = smtModelFactory;
        _corpusService = corpusService;
        _logger = logger;
    }

    [Queue("smt_transfer")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken externalCancellationToken
    )
    {
        IDistributedReaderWriterLock rwLock = _lockFactory.Create(engineId);

        var tokenizer = new LatinWordTokenizer();
        var detokenizer = new LatinWordDetokenizer();
        ITrainer? smtModelTrainer = null;
        ITrainer? truecaseTrainer = null;
        try
        {
            CancellationTokenSource cts = new();
            SubscribeForCancellationAsync(cts, engineId, buildId);
            CancellationTokenSource combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                externalCancellationToken,
                cts.Token
            );
            var combinedCancellationToken = combinedCancellationSource.Token;

            var stopwatch = new Stopwatch();
            TranslationEngine? engine;
            await using (await rwLock.WriterLockAsync(cancellationToken: combinedCancellationToken))
            {
                engine = await _engines.UpdateAsync(
                    e => e.EngineId == engineId && e.BuildId == buildId && !e.IsCanceled,
                    u => u.Set(e => e.BuildState, BuildState.Active),
                    cancellationToken: combinedCancellationToken
                );
                if (engine is null)
                    throw new OperationCanceledException();

                await _platformService.BuildStartedAsync(buildId, combinedCancellationToken);
                _logger.LogInformation("Build started ({0})", buildId);
                stopwatch.Start();

                await _trainSegmentPairs.DeleteAllAsync(
                    p => p.TranslationEngineRef == engineId,
                    combinedCancellationToken
                );

                combinedCancellationToken.ThrowIfCancellationRequested();

                var targetCorpora = new List<ITextCorpus>();
                var parallelCorpora = new List<IParallelTextCorpus>();
                foreach (Corpus corpus in corpora)
                {
                    ITextCorpus sc = _corpusService.CreateTextCorpus(corpus.SourceFiles);
                    ITextCorpus tc = _corpusService.CreateTextCorpus(corpus.TargetFiles);

                    targetCorpora.Add(tc);
                    parallelCorpora.Add(sc.AlignRows(tc));
                }

                IParallelTextCorpus parallelCorpus = parallelCorpora.Flatten();
                ITextCorpus targetCorpus = targetCorpora.Flatten();

                smtModelTrainer = _smtModelFactory.CreateTrainer(engineId, tokenizer, parallelCorpus);
                truecaseTrainer = _truecaserFactory.CreateTrainer(engineId, tokenizer, targetCorpus);
            }

            combinedCancellationToken.ThrowIfCancellationRequested();

            var progress = new BuildProgress(_platformService, buildId);
            await smtModelTrainer.TrainAsync(progress, combinedCancellationToken);
            await truecaseTrainer.TrainAsync(cancellationToken: combinedCancellationToken);
            int trainSegmentPairCount;
            await using (await rwLock.WriterLockAsync(cancellationToken: combinedCancellationToken))
            {
                combinedCancellationToken.ThrowIfCancellationRequested();
                await smtModelTrainer.SaveAsync(combinedCancellationToken);
                await truecaseTrainer.SaveAsync(combinedCancellationToken);
                combinedCancellationToken.ThrowIfCancellationRequested();
                ITruecaser truecaser = await _truecaserFactory.CreateAsync(engineId);
                IReadOnlyList<TrainSegmentPair> segmentPairs = await _trainSegmentPairs.GetAllAsync(
                    p => p.TranslationEngineRef == engine!.Id,
                    combinedCancellationToken
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
                            cancellationToken: combinedCancellationToken
                        );
                        combinedCancellationToken.ThrowIfCancellationRequested();
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

    private async void SubscribeForCancellationAsync(CancellationTokenSource cts, string engineId, string buildId)
    {
        var cancellationToken = cts.Token;
        ISubscription<TranslationEngine> sub = await _engines.SubscribeAsync(
            e => e.EngineId == engineId && e.BuildId == buildId
        );
        if (sub.Change.Entity is null)
            return;
        while (true)
        {
            await sub.WaitForChangeAsync(TimeSpan.FromSeconds(10), cancellationToken);
            TranslationEngine? engine = sub.Change.Entity;
            if (engine is null || engine.IsCanceled)
            {
                cts.Cancel();
                return;
            }
            if (cancellationToken.IsCancellationRequested)
                return;
            Thread.Sleep(500);
        }
    }
}
