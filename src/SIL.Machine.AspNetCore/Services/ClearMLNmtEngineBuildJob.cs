namespace SIL.Machine.AspNetCore.Services;

public class ClearMLNmtEngineBuildJob
{
    private readonly IPlatformService _platformService;
    private readonly IRepository<TranslationEngine> _engines;
    private readonly ILogger<ClearMLNmtEngineBuildJob> _logger;
    private readonly IClearMLService _clearMLService;
    private readonly ISharedFileService _sharedFileService;
    private readonly IOptionsMonitor<ClearMLNmtEngineOptions> _options;
    private readonly ICorpusService _corpusService;

    public ClearMLNmtEngineBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        ILogger<ClearMLNmtEngineBuildJob> logger,
        IClearMLService clearMLService,
        ISharedFileService sharedFileService,
        IOptionsMonitor<ClearMLNmtEngineOptions> options,
        ICorpusService corpusService
    )
    {
        _platformService = platformService;
        _engines = engines;
        _logger = logger;
        _clearMLService = clearMLService;
        _sharedFileService = sharedFileService;
        _options = options;
        _corpusService = corpusService;
    }

    [Queue("nmt")]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(
        string engineId,
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken externalCancellationToken
    )
    {
        string? clearMLProjectId = await _clearMLService.GetProjectIdAsync(engineId, externalCancellationToken);
        if (clearMLProjectId is null)
            return;

        try
        {
            var combinedCancellationToken = new SubscribeForCancellation(_engines).GetCombinedCancellationToken(
                engineId,
                buildId,
                externalCancellationToken
            );

            TranslationEngine? engine = await _engines.GetAsync(
                e => e.EngineId == engineId && e.BuildId == buildId,
                cancellationToken: combinedCancellationToken
            );
            if (engine is null || engine.IsCanceled)
                throw new OperationCanceledException();

            int corpusSize;
            if (engine.BuildState is BuildState.Pending)
                corpusSize = await WriteDataFilesAsync(buildId, corpora, combinedCancellationToken);
            else
                corpusSize = GetCorpusSize(corpora);

            string clearMLTaskId;
            ClearMLTask? clearMLTask = await _clearMLService.GetTaskByNameAsync(buildId, combinedCancellationToken);
            if (clearMLTask is null)
            {
                clearMLTaskId = await _clearMLService.CreateTaskAsync(
                    buildId,
                    clearMLProjectId,
                    engineId,
                    engine.SourceLanguage,
                    engine.TargetLanguage,
                    _sharedFileService.GetBaseUri().ToString(),
                    combinedCancellationToken
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
                combinedCancellationToken.ThrowIfCancellationRequested();

                clearMLTask = await _clearMLService.GetTaskByIdAsync(clearMLTaskId, combinedCancellationToken);
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
                        cancellationToken: combinedCancellationToken
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
                await Task.Delay(_options.CurrentValue.BuildPollingTimeout, combinedCancellationToken);
            }

            // The ClearML task has successfully completed, so insert the generated pretranslations into the database.
            await InsertPretranslationsAsync(engineId, buildId, combinedCancellationToken);

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

            if (!metrics.TryGetValue("bleu", out double confidence))
                confidence = 0;

            await _platformService.BuildCompletedAsync(
                buildId,
                corpusSize,
                Math.Round(confidence, 2, MidpointRounding.AwayFromZero),
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
                // This is an actual cancellation triggered by an API call.
                ClearMLTask? task = await _clearMLService.GetTaskByNameAsync(buildId, CancellationToken.None);
                if (task is not null)
                    await _clearMLService.StopTaskAsync(task.Id, CancellationToken.None);

                await _sharedFileService.DeleteAsync($"builds/{buildId}/", CancellationToken.None);

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
            else if (engine is not null)
            {
                // the build was canceled, because of a server shutdown
                // switch state back to pending
                await _platformService.BuildRestartingAsync(buildId, CancellationToken.None);
            }

            throw;
        }
        catch (Exception e)
        {
            try
            {
                await _sharedFileService.DeleteAsync($"builds/{buildId}/", CancellationToken.None);
            }
            catch (HttpRequestException)
            {
                _logger.LogError("Unable to access S3 bucket. Likely, the AWS credentials are invalid.");
            }
            finally
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

                await _platformService.BuildFaultedAsync(buildId, e.Message, CancellationToken.None);
                _logger.LogError(0, e, "Build faulted ({0})", buildId);
                throw e;
            }
        }
    }

    private async Task<int> WriteDataFilesAsync(
        string buildId,
        IReadOnlyList<Corpus> corpora,
        CancellationToken cancellationToken
    )
    {
        await using var sourceTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.src.txt", cancellationToken)
        );
        await using var targetTrainWriter = new StreamWriter(
            await _sharedFileService.OpenWriteAsync($"builds/{buildId}/train.trg.txt", cancellationToken)
        );

        int corpusSize = 0;
        async IAsyncEnumerable<Pretranslation> ProcessRowsAsync()
        {
            foreach (Corpus corpus in corpora)
            {
                ITextCorpus sourceCorpus = _corpusService.CreateTextCorpus(corpus.SourceFiles);
                ITextCorpus targetCorpus = _corpusService.CreateTextCorpus(corpus.TargetFiles);

                IParallelTextCorpus parallelCorpus = sourceCorpus.AlignRows(
                    targetCorpus,
                    allSourceRows: true,
                    allTargetRows: true
                );

                foreach (ParallelTextRow row in parallelCorpus)
                {
                    await sourceTrainWriter.WriteAsync($"{row.SourceText}\n");
                    await targetTrainWriter.WriteAsync($"{row.TargetText}\n");
                    if (
                        (corpus.PretranslateAll || corpus.PretranslateTextIds.Contains(row.TextId))
                        && row.SourceSegment.Count > 0
                    )
                    {
                        yield return new Pretranslation
                        {
                            CorpusId = corpus.Id,
                            TextId = row.TextId,
                            Refs = row.TargetRefs.Select(r => r.ToString()!).ToList(),
                            Translation = row.SourceText
                        };
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

    private int GetCorpusSize(IReadOnlyList<Corpus> corpora)
    {
        int corpusSize = 0;
        foreach (Corpus corpus in corpora)
        {
            ITextCorpus sourceCorpus = _corpusService.CreateTextCorpus(corpus.SourceFiles);
            ITextCorpus targetCorpus = _corpusService.CreateTextCorpus(corpus.TargetFiles);

            IParallelTextCorpus parallelCorpus = sourceCorpus.AlignRows(targetCorpus);

            corpusSize += parallelCorpus.Count(includeEmpty: false);
        }
        return corpusSize;
    }

    private async Task InsertPretranslationsAsync(string engineId, string buildId, CancellationToken cancellationToken)
    {
        await using var targetPretranslateStream = await _sharedFileService.OpenReadAsync(
            $"builds/{buildId}/pretranslate.trg.json",
            cancellationToken
        );

        IAsyncEnumerable<Pretranslation> pretranslations = JsonSerializer
            .DeserializeAsyncEnumerable<Pretranslation>(
                targetPretranslateStream,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                cancellationToken
            )
            .OfType<Pretranslation>();

        await _platformService.InsertPretranslationsAsync(engineId, pretranslations, cancellationToken);
    }
}
