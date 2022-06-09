namespace SIL.Machine.WebApi.Services;

public class ClearMLNmtEngineBuildJob
{
    private const int PretranslationInsertBatchSize = 128;

    private readonly IRepository<TranslationEngine> _engines;
    private readonly IRepository<Build> _builds;
    private readonly IRepository<Pretranslation> _pretranslations;
    private readonly ILogger<ClearMLNmtEngineBuildJob> _logger;
    private readonly IWebhookService _webhookService;
    private readonly IClearMLService _clearMLService;
    private readonly ICorpusService _corpusService;
    private readonly ISharedFileService _sharedFileService;
    private readonly IOptionsMonitor<ClearMLOptions> _options;

    public ClearMLNmtEngineBuildJob(
        IRepository<TranslationEngine> engines,
        IRepository<Build> builds,
        IRepository<Pretranslation> pretranslations,
        ILogger<ClearMLNmtEngineBuildJob> logger,
        IWebhookService webhookService,
        IClearMLService clearMLService,
        ICorpusService corpusService,
        ISharedFileService sharedFileService,
        IOptionsMonitor<ClearMLOptions> options
    )
    {
        _engines = engines;
        _builds = builds;
        _pretranslations = pretranslations;
        _logger = logger;
        _webhookService = webhookService;
        _clearMLService = clearMLService;
        _corpusService = corpusService;
        _sharedFileService = sharedFileService;
        _options = options;
    }

    [Queue("nmt")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        TranslationEngine? engine = await _engines.GetAsync(engineId, cancellationToken);
        if (engine is null)
            return;

        string? clearMLProjectId = await _clearMLService.GetProjectIdAsync(engineId, cancellationToken);
        if (clearMLProjectId is null)
            return;

        try
        {
            Build? build = await _builds.GetAsync(buildId, cancellationToken: cancellationToken);
            if (build is null || build.State is BuildState.Canceled)
                throw new OperationCanceledException();

            int corpusSize;
            if (build.State is BuildState.Pending)
                corpusSize = await WriteDataFilesAsync(engine, buildId, cancellationToken);
            else
                corpusSize = await GetCorpusSizeAsync(engine);

            string clearMLTaskId;
            ClearMLTask? clearMLTask = await _clearMLService.GetTaskAsync(buildId, clearMLProjectId, cancellationToken);
            if (clearMLTask is null)
            {
                clearMLTaskId = await _clearMLService.CreateTaskAsync(
                    buildId,
                    clearMLProjectId,
                    engine.SourceLanguageTag,
                    engine.TargetLanguageTag,
                    _sharedFileService.GetUri(buildId),
                    cancellationToken
                );
                await _clearMLService.EnqueueTaskAsync(clearMLTaskId, CancellationToken.None);
            }
            else
            {
                clearMLTaskId = clearMLTask.Id;
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                clearMLTask = await _clearMLService.GetTaskAsync(clearMLTaskId, cancellationToken);
                if (clearMLTask is null)
                    throw new InvalidOperationException("The ClearML task does not exist.");

                if (
                    build.State == BuildState.Pending
                    && clearMLTask.Status
                        is ClearMLTaskStatus.InProgress
                            or ClearMLTaskStatus.Stopped
                            or ClearMLTaskStatus.Failed
                            or ClearMLTaskStatus.Completed
                )
                {
                    build = await _builds.UpdateAsync(
                        b => b.Id == buildId && b.State != BuildState.Canceled,
                        u => u.Set(b => b.State, BuildState.Active),
                        cancellationToken: cancellationToken
                    );
                    if (build is null)
                        throw new OperationCanceledException();

                    await _engines.UpdateAsync(
                        engineId,
                        u => u.Set(e => e.IsBuilding, true),
                        cancellationToken: CancellationToken.None
                    );

                    await _webhookService.SendEventAsync(WebhookEvent.BuildStarted, engine.Owner, build);
                    _logger.LogInformation("Build started ({0})", buildId);
                }

                switch (clearMLTask.Status)
                {
                    case ClearMLTaskStatus.InProgress:
                    case ClearMLTaskStatus.Completed:
                        if (build.Step != clearMLTask.LastIteration)
                        {
                            build = await _builds.UpdateAsync(
                                build,
                                u => u.Set(b => b.Step, clearMLTask.LastIteration),
                                cancellationToken: cancellationToken
                            );
                            if (build is null)
                                throw new OperationCanceledException();
                        }
                        break;
                    case ClearMLTaskStatus.Stopped:
                        // This could have been triggered from the ClearML UI, so change the state to Canceled.
                        await _builds.UpdateAsync(
                            b => b.Id == buildId && b.State != BuildState.Canceled,
                            u => u.Set(b => b.State, BuildState.Canceled),
                            cancellationToken: CancellationToken.None
                        );
                        throw new OperationCanceledException();
                    case ClearMLTaskStatus.Failed:
                        throw new InvalidOperationException(clearMLTask.StatusReason);
                }
                if (clearMLTask.Status is ClearMLTaskStatus.Completed)
                    break;
                await Task.Delay(_options.CurrentValue.BuildPollingTimeout, cancellationToken);
            }

            // The ClearML task has successfully completed, so insert the generated pretranslations into the database.
            await InsertPretranslationsAsync(engineId, buildId, cancellationToken);

            IReadOnlyDictionary<string, double> metrics = await _clearMLService.GetTaskMetricsAsync(
                clearMLTaskId,
                CancellationToken.None
            );

            await _sharedFileService.DeleteAsync($"{buildId}/", CancellationToken.None);

            await _engines.UpdateAsync(
                engineId,
                u =>
                    u.Set(e => e.IsBuilding, false)
                        .Inc(e => e.ModelRevision)
                        .Set(e => e.Confidence, Math.Round(metrics["bleu"], 2, MidpointRounding.AwayFromZero))
                        .Set(e => e.CorpusSize, corpusSize),
                cancellationToken: CancellationToken.None
            );

            build = await _builds.UpdateAsync(
                buildId,
                u =>
                    u.Set(b => b.State, BuildState.Completed)
                        .Set(b => b.Message, "Completed")
                        .Set(b => b.DateFinished, DateTime.UtcNow),
                cancellationToken: CancellationToken.None
            );

            _logger.LogInformation("Build completed in {0}s ({1})", clearMLTask.ActiveDuration, buildId);
            await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
        }
        catch (OperationCanceledException)
        {
            // Check if the cancellation was initiated by an API call or a shutdown.
            Build? build = await _builds.GetAsync(buildId, CancellationToken.None);
            if (build?.State is BuildState.Canceled)
            {
                ClearMLTask? task = await _clearMLService.GetTaskAsync(
                    buildId,
                    clearMLProjectId,
                    CancellationToken.None
                );
                if (task is not null)
                    await _clearMLService.StopTaskAsync(task.Id, CancellationToken.None);

                await _sharedFileService.DeleteAsync($"{buildId}/", CancellationToken.None);

                // This is an actual cancellation triggered by an API call.
                bool buildStarted =
                    (
                        await _engines.UpdateAsync(
                            e => e.Id == engineId && e.IsBuilding,
                            u => u.Set(e => e.IsBuilding, false),
                            cancellationToken: CancellationToken.None
                        )
                    )
                        is not null;

                build =
                    await _builds.UpdateAsync(
                        b => b.Id == buildId && b.DateFinished == null,
                        u => u.Set(b => b.DateFinished, DateTime.UtcNow),
                        cancellationToken: CancellationToken.None
                    ) ?? build;

                if (buildStarted)
                {
                    _logger.LogInformation("Build canceled ({0})", buildId);
                    await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
                }
            }

            throw;
        }
        catch (Exception e)
        {
            await _sharedFileService.DeleteAsync($"{buildId}/", CancellationToken.None);

            await _engines.UpdateAsync(
                e => e.Id == engineId && e.IsBuilding,
                u => u.Set(e => e.IsBuilding, false),
                cancellationToken: CancellationToken.None
            );

            Build? build = await _builds.UpdateAsync(
                buildId,
                u =>
                    u.Set(b => b.State, BuildState.Faulted)
                        .Set(b => b.Message, e.Message)
                        .Set(b => b.DateFinished, DateTime.UtcNow),
                cancellationToken: CancellationToken.None
            );

            _logger.LogError(0, e, "Build faulted ({0})", buildId);
            await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
            throw;
        }
    }

    private async Task<int> WriteDataFilesAsync(
        TranslationEngine engine,
        string buildId,
        CancellationToken cancellationToken
    )
    {
        await using var sourceTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"{buildId}/train.src.txt", cancellationToken)
        );
        await using var targetTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"{buildId}/train.trg.txt", cancellationToken)
        );

        int corpusSize = 0;
        async IAsyncEnumerable<PretranslationInfo> ProcessRowsAsync()
        {
            foreach (TranslationEngineCorpus corpus in engine.Corpora)
            {
                ITextCorpus sourceCorpus =
                    await _corpusService.CreateTextCorpusAsync(corpus.CorpusRef, engine.SourceLanguageTag)
                    ?? new DictionaryTextCorpus();
                ITextCorpus targetCorpus =
                    await _corpusService.CreateTextCorpusAsync(corpus.CorpusRef, engine.TargetLanguageTag)
                    ?? new DictionaryTextCorpus();

                IParallelTextCorpus parallelCorpus = sourceCorpus.AlignRows(
                    targetCorpus,
                    allSourceRows: true,
                    allTargetRows: true
                );

                foreach (ParallelTextRow row in parallelCorpus)
                {
                    await sourceTrainWriter.WriteAsync($"{row.SourceText}\n");
                    await targetTrainWriter.WriteAsync($"{row.TargetText}\n");
                    if (corpus.Pretranslate && row.SourceSegment.Count > 0 && row.TargetSegment.Count == 0)
                    {
                        yield return new PretranslationInfo
                        {
                            CorpusId = corpus.CorpusRef,
                            TextId = row.TextId,
                            Refs = row.TargetRefs.Select(r => r.ToString()!).ToList(),
                            Segment = row.SourceText
                        };
                    }
                    if (!row.IsEmpty)
                        corpusSize++;
                }
            }
        }

        await using var sourcePretranslateStream = await _sharedFileService.OpenWriteAsync(
            $"{buildId}/pretranslate.src.json",
            cancellationToken
        );

        await JsonSerializer.SerializeAsync(
            sourcePretranslateStream,
            ProcessRowsAsync(),
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            cancellationToken: cancellationToken
        );
        return corpusSize;
    }

    private async Task<int> GetCorpusSizeAsync(TranslationEngine engine)
    {
        int corpusSize = 0;
        foreach (TranslationEngineCorpus corpus in engine.Corpora)
        {
            ITextCorpus? sourceCorpus = await _corpusService.CreateTextCorpusAsync(
                corpus.CorpusRef,
                engine.SourceLanguageTag
            );
            if (sourceCorpus is null)
                continue;
            ITextCorpus? targetCorpus = await _corpusService.CreateTextCorpusAsync(
                corpus.CorpusRef,
                engine.TargetLanguageTag
            );
            if (targetCorpus is null)
                continue;

            IParallelTextCorpus parallelCorpus = sourceCorpus.AlignRows(targetCorpus);

            corpusSize += parallelCorpus.Count(includeEmpty: false);
        }
        return corpusSize;
    }

    private async Task InsertPretranslationsAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        await _pretranslations.DeleteAllAsync(p => p.TranslationEngineRef == engineId, cancellationToken);

        await using var targetPretranslateStream = await _sharedFileService.OpenReadAsync(
            $"{buildId}/pretranslate.trg.json",
            cancellationToken
        );
        var batch = new List<Pretranslation>();
        await foreach (
            PretranslationInfo? pi in JsonSerializer.DeserializeAsyncEnumerable<PretranslationInfo>(
                targetPretranslateStream,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                cancellationToken
            )
        )
        {
            if (pi is null)
                continue;

            batch.Add(
                new Pretranslation
                {
                    TranslationEngineRef = engineId,
                    CorpusRef = pi.CorpusId,
                    TextId = pi.TextId,
                    Refs = pi.Refs,
                    Translation = pi.Segment,
                }
            );
            if (batch.Count == PretranslationInsertBatchSize)
            {
                await _pretranslations.InsertAllAsync(batch, cancellationToken);
                batch.Clear();
            }
        }
        if (batch.Count > 0)
            await _pretranslations.InsertAllAsync(batch, cancellationToken);
    }

    private class PretranslationInfo
    {
        public string CorpusId { get; set; } = default!;
        public string TextId { get; set; } = default!;
        public List<string> Refs { get; set; } = default!;
        public string Segment { get; set; } = default!;
    }
}
