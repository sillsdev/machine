namespace SIL.Machine.AspNetCore.Services;

public class ClearMLNmtEngineBuildJob
{
    private readonly IPlatformService _platformService;
    private readonly IRepository<TranslationEngine> _engines;
    private readonly ILogger<ClearMLNmtEngineBuildJob> _logger;
    private readonly IClearMLService _clearMLService;
    private readonly ISharedFileService _sharedFileService;
    private readonly IOptionsMonitor<ClearMLOptions> _options;

    public ClearMLNmtEngineBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        ILogger<ClearMLNmtEngineBuildJob> logger,
        IClearMLService clearMLService,
        ISharedFileService sharedFileService,
        IOptionsMonitor<ClearMLOptions> options
    )
    {
        _platformService = platformService;
        _engines = engines;
        _logger = logger;
        _clearMLService = clearMLService;
        _sharedFileService = sharedFileService;
        _options = options;
    }

    [Queue("nmt")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        string? clearMLProjectId = await _clearMLService.GetProjectIdAsync(engineId, cancellationToken);
        if (clearMLProjectId is null)
            return;

        TranslationEngineInfo? engineInfo = await _platformService.GetTranslationEngineInfoAsync(
            engineId,
            cancellationToken
        );
        if (engineInfo is null)
            return;

        try
        {
            TranslationEngine? engine = await _engines.GetAsync(
                e => e.EngineId == engineId && e.BuildId == buildId,
                cancellationToken: cancellationToken
            );
            if (engine is null || engine.IsCanceled)
                throw new OperationCanceledException();

            int corpusSize;
            if (engine.BuildState is BuildState.Pending)
                corpusSize = await WriteDataFilesAsync(engineId, buildId, cancellationToken);
            else
                corpusSize = await GetCorpusSizeAsync(engineId, cancellationToken);

            string clearMLTaskId;
            ClearMLTask? clearMLTask = await _clearMLService.GetTaskAsync(buildId, clearMLProjectId, cancellationToken);
            if (clearMLTask is null)
            {
                clearMLTaskId = await _clearMLService.CreateTaskAsync(
                    buildId,
                    clearMLProjectId,
                    engineId,
                    engineInfo.SourceLanguageTag,
                    engineInfo.TargetLanguageTag,
                    cancellationToken
                );
                await _clearMLService.EnqueueTaskAsync(clearMLTaskId, CancellationToken.None);
            }
            else
            {
                clearMLTaskId = clearMLTask.Id;
            }

            int lastIteration = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                clearMLTask = await _clearMLService.GetTaskAsync(clearMLTaskId, cancellationToken);
                if (clearMLTask is null)
                    throw new InvalidOperationException("The ClearML task does not exist.");

                if (
                    engine.BuildState == BuildState.Pending
                    && clearMLTask.Status
                        is ClearMLTaskStatus.InProgress
                            or ClearMLTaskStatus.Stopped
                            or ClearMLTaskStatus.Failed
                            or ClearMLTaskStatus.Completed
                )
                {
                    engine = await _engines.UpdateAsync(
                        e => e.EngineId == engineId && e.BuildId == buildId && !e.IsCanceled,
                        u => u.Set(e => e.BuildState, BuildState.Active),
                        cancellationToken: cancellationToken
                    );
                    if (engine is null)
                        throw new OperationCanceledException();
                    await _platformService.BuildStartedAsync(buildId, CancellationToken.None);
                    _logger.LogInformation("Build started ({0})", buildId);
                }

                switch (clearMLTask.Status)
                {
                    case ClearMLTaskStatus.InProgress:
                    case ClearMLTaskStatus.Completed:
                        if (lastIteration != clearMLTask.LastIteration)
                        {
                            await _platformService.UpdateBuildStatusAsync(buildId, clearMLTask.LastIteration);
                            lastIteration = clearMLTask.LastIteration;
                        }
                        break;
                    case ClearMLTaskStatus.Stopped:
                        // This could have been triggered from the ClearML UI, so set IsCanceled to true.
                        await _engines.UpdateAsync(
                            e => e.EngineId == engineId && !e.IsCanceled,
                            u => u.Set(e => e.IsCanceled, true),
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

            await _sharedFileService.DeleteAsync($"builds/{buildId}/", CancellationToken.None);

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

            await _platformService.BuildCompletedAsync(
                buildId,
                corpusSize,
                Math.Round(metrics["bleu"], 2, MidpointRounding.AwayFromZero),
                CancellationToken.None
            );
            _logger.LogInformation("Build completed in {0}s ({1})", clearMLTask.ActiveDuration, buildId);
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
                ClearMLTask? task = await _clearMLService.GetTaskAsync(
                    buildId,
                    clearMLProjectId,
                    CancellationToken.None
                );
                if (task is not null)
                    await _clearMLService.StopTaskAsync(task.Id, CancellationToken.None);

                await _sharedFileService.DeleteAsync($"builds/{buildId}/", CancellationToken.None);

                // This is an actual cancellation triggered by an API call.
                bool buildStarted = await _engines.ExistsAsync(
                    e => e.EngineId == engineId && e.BuildId == buildId && e.BuildState == BuildState.Active,
                    CancellationToken.None
                );

                await _engines.UpdateAsync(
                    e => e.EngineId == engineId && e.BuildId == buildId,
                    u =>
                        u.Set(e => e.BuildState, BuildState.None)
                            .Set(e => e.IsCanceled, false)
                            .Unset(e => e.JobId)
                            .Unset(e => e.BuildId),
                    cancellationToken: CancellationToken.None
                );

                if (buildStarted)
                {
                    await _platformService.BuildCanceledAsync(buildId, CancellationToken.None);
                    _logger.LogInformation("Build canceled ({0})", buildId);
                }
            }

            throw;
        }
        catch (Exception e)
        {
            await _sharedFileService.DeleteAsync($"builds/{buildId}/", CancellationToken.None);

            await _engines.UpdateAsync(
                e => e.EngineId == engineId && e.BuildId == buildId,
                u =>
                    u.Set(e => e.BuildState, BuildState.None)
                        .Set(e => e.IsCanceled, false)
                        .Unset(e => e.JobId)
                        .Unset(e => e.BuildId),
                cancellationToken: CancellationToken.None
            );

            await _platformService.BuildFaultedAsync(buildId, e.Message, CancellationToken.None);
            _logger.LogError(0, e, "Build faulted ({0})", buildId);
            throw;
        }
    }

    private async Task<int> WriteDataFilesAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        await using var sourceTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.src.txt", cancellationToken)
        );
        await using var targetTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.trg.txt", cancellationToken)
        );

        int corpusSize = 0;
        async IAsyncEnumerable<PretranslationInfo> ProcessRowsAsync()
        {
            await foreach (CorpusInfo corpus in _platformService.GetCorporaAsync(engineId, cancellationToken))
            {
                ITextCorpus sourceCorpus = corpus.SourceCorpus ?? new DictionaryTextCorpus();
                ITextCorpus targetCorpus = corpus.TargetCorpus ?? new DictionaryTextCorpus();

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
                        yield return new(
                            corpus.Id,
                            row.TextId,
                            row.TargetRefs.Select(r => r.ToString()!).ToList(),
                            row.SourceText
                        );
                    }
                    if (!row.IsEmpty)
                        corpusSize++;
                }
            }
        }

        await using var sourcePretranslateStream = await _sharedFileService.OpenWriteAsync(
            $"builds/{buildId}/pretranslate.src.json",
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

    private async Task<int> GetCorpusSizeAsync(string engineId, CancellationToken cancellationToken = default)
    {
        int corpusSize = 0;
        await foreach (CorpusInfo corpus in _platformService.GetCorporaAsync(engineId, cancellationToken))
        {
            if (corpus.SourceCorpus is null || corpus.TargetCorpus is null)
                continue;

            IParallelTextCorpus parallelCorpus = corpus.SourceCorpus.AlignRows(corpus.TargetCorpus);

            corpusSize += parallelCorpus.Count(includeEmpty: false);
        }
        return corpusSize;
    }

    private async Task InsertPretranslationsAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        await _platformService.DeleteAllPretranslationsAsync(engineId, cancellationToken);

        await using var targetPretranslateStream = await _sharedFileService.OpenReadAsync(
            $"builds/{buildId}/pretranslate.trg.json",
            cancellationToken
        );

        IAsyncEnumerable<PretranslationInfo> pretranslations = JsonSerializer
            .DeserializeAsyncEnumerable<PretranslationInfo>(
                targetPretranslateStream,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                cancellationToken
            )
            .OfType<PretranslationInfo>();

        await _platformService.InsertPretranslationsAsync(engineId, pretranslations, cancellationToken);
    }
}
