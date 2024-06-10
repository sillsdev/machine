namespace SIL.Machine.AspNetCore.Services;

public class ClearMLMonitorService(
    IServiceProvider services,
    IClearMLService clearMLService,
    ISharedFileService sharedFileService,
    IOptionsMonitor<ClearMLOptions> clearMLOptions,
    IOptionsMonitor<BuildJobOptions> buildJobOptions,
    ILogger<ClearMLMonitorService> logger
)
    : RecurrentTask(
        "ClearML monitor service",
        services,
        clearMLOptions.CurrentValue.BuildPollingTimeout,
        logger,
        clearMLOptions.CurrentValue.BuildPollingEnabled
    ),
        IClearMLQueueService
{
    private static readonly string SummaryMetric = CreateMD5("Summary");
    private static readonly string TrainCorpusSizeVariant = CreateMD5("train_corpus_size");
    private static readonly string ConfidenceVariant = CreateMD5("confidence");

    private readonly IClearMLService _clearMLService = clearMLService;
    private readonly ISharedFileService _sharedFileService = sharedFileService;
    private readonly ILogger<IClearMLQueueService> _logger = logger;
    private readonly Dictionary<string, ProgressStatus> _curBuildStatus = new();

    private readonly IReadOnlyDictionary<TranslationEngineType, string> _queuePerEngineType =
        buildJobOptions.CurrentValue.ClearML.ToDictionary(x => x.TranslationEngineType, x => x.Queue);

    private readonly IDictionary<TranslationEngineType, int> _queueSizePerEngineType = new ConcurrentDictionary<
        TranslationEngineType,
        int
    >(buildJobOptions.CurrentValue.ClearML.ToDictionary(x => x.TranslationEngineType, x => 0));

    public int GetQueueSize(TranslationEngineType engineType)
    {
        return _queueSizePerEngineType[engineType];
    }

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var buildJobService = scope.ServiceProvider.GetRequiredService<IBuildJobService>();
            IReadOnlyList<TranslationEngine> trainingEngines = await buildJobService.GetBuildingEnginesAsync(
                BuildJobRunnerType.ClearML,
                cancellationToken
            );
            if (trainingEngines.Count == 0)
                return;

            Dictionary<string, ClearMLTask> tasks = new();
            Dictionary<string, int> queuePositions = new();

            foreach (TranslationEngineType engineType in _queuePerEngineType.Keys)
            {
                Dictionary<string, ClearMLTask> tasksPerEngineType = (
                    await _clearMLService.GetTasksByIdAsync(
                        trainingEngines.Select(e => e.CurrentBuild!.JobId),
                        cancellationToken
                    )
                )
                    .UnionBy(
                        await _clearMLService.GetTasksForQueueAsync(_queuePerEngineType[engineType], cancellationToken),
                        t => t.Id
                    )
                    .ToDictionary(t => t.Id);
                // add new keys to dictionary
                foreach (KeyValuePair<string, ClearMLTask> kvp in tasksPerEngineType)
                    tasks.TryAdd(kvp.Key, kvp.Value);

                Dictionary<string, int> queuePositionsPerEngineType = tasksPerEngineType
                    .Values.Where(t => t.Status is ClearMLTaskStatus.Queued or ClearMLTaskStatus.Created)
                    .OrderBy(t => t.Created)
                    .Select((t, i) => (Position: i, Task: t))
                    .ToDictionary(e => e.Task.Name, e => e.Position);
                // add new keys to dictionary
                foreach (KeyValuePair<string, int> kvp in queuePositionsPerEngineType)
                    queuePositions.TryAdd(kvp.Key, kvp.Value);

                _queueSizePerEngineType[engineType] = queuePositionsPerEngineType.Count;
            }

            var platformService = scope.ServiceProvider.GetRequiredService<IPlatformService>();
            var lockFactory = scope.ServiceProvider.GetRequiredService<IDistributedReaderWriterLockFactory>();
            foreach (TranslationEngine engine in trainingEngines)
            {
                if (engine.CurrentBuild is null || !tasks.TryGetValue(engine.CurrentBuild.JobId, out ClearMLTask? task))
                    continue;

                if (
                    engine.CurrentBuild.JobState is BuildJobState.Pending
                    && task.Status is ClearMLTaskStatus.Queued or ClearMLTaskStatus.Created
                )
                {
                    await UpdateTrainJobStatus(
                        platformService,
                        engine.CurrentBuild.BuildId,
                        new ProgressStatus(step: 0, percentCompleted: 0.0),
                        //CurrentBuild.BuildId should always equal the corresponding task.Name
                        queuePositions[engine.CurrentBuild.BuildId] + 1,
                        cancellationToken
                    );
                }

                if (engine.CurrentBuild.Stage == BuildStage.Train)
                {
                    if (
                        engine.CurrentBuild.JobState is BuildJobState.Pending
                        && task.Status
                            is ClearMLTaskStatus.InProgress
                                or ClearMLTaskStatus.Stopped
                                or ClearMLTaskStatus.Failed
                                or ClearMLTaskStatus.Completed
                    )
                    {
                        bool canceled = !await TrainJobStartedAsync(
                            lockFactory,
                            buildJobService,
                            platformService,
                            engine.EngineId,
                            engine.CurrentBuild.BuildId,
                            cancellationToken
                        );
                        if (canceled)
                            continue;
                    }

                    switch (task.Status)
                    {
                        case ClearMLTaskStatus.InProgress:
                        {
                            double? percentCompleted = null;
                            if (task.Runtime.TryGetValue("progress", out string? progressStr))
                                percentCompleted = int.Parse(progressStr, CultureInfo.InvariantCulture) / 100.0;
                            task.Runtime.TryGetValue("message", out string? message);
                            await UpdateTrainJobStatus(
                                platformService,
                                engine.CurrentBuild.BuildId,
                                new ProgressStatus(task.LastIteration ?? 0, percentCompleted, message),
                                queueDepth: 0,
                                cancellationToken
                            );
                            break;
                        }

                        case ClearMLTaskStatus.Completed:
                        {
                            task.Runtime.TryGetValue("message", out string? message);
                            await UpdateTrainJobStatus(
                                platformService,
                                engine.CurrentBuild.BuildId,
                                new ProgressStatus(task.LastIteration ?? 0, percentCompleted: 1.0, message),
                                queueDepth: 0,
                                cancellationToken
                            );
                            bool canceling = !await TrainJobCompletedAsync(
                                lockFactory,
                                buildJobService,
                                engine.EngineId,
                                engine.CurrentBuild.BuildId,
                                (int)GetMetric(task, SummaryMetric, TrainCorpusSizeVariant),
                                GetMetric(task, SummaryMetric, ConfidenceVariant),
                                engine.CurrentBuild.Options,
                                cancellationToken
                            );
                            if (canceling)
                            {
                                await TrainJobCanceledAsync(
                                    lockFactory,
                                    buildJobService,
                                    platformService,
                                    engine.EngineId,
                                    engine.CurrentBuild.BuildId,
                                    cancellationToken
                                );
                            }
                            break;
                        }

                        case ClearMLTaskStatus.Stopped:
                        {
                            await TrainJobCanceledAsync(
                                lockFactory,
                                buildJobService,
                                platformService,
                                engine.EngineId,
                                engine.CurrentBuild.BuildId,
                                cancellationToken
                            );
                            break;
                        }

                        case ClearMLTaskStatus.Failed:
                        {
                            await TrainJobFaultedAsync(
                                lockFactory,
                                buildJobService,
                                platformService,
                                engine.EngineId,
                                engine.CurrentBuild.BuildId,
                                $"{task.StatusReason} : {task.StatusMessage}",
                                cancellationToken
                            );
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while monitoring ClearML tasks.");
        }
    }

    private async Task<bool> TrainJobStartedAsync(
        IDistributedReaderWriterLockFactory lockFactory,
        IBuildJobService buildJobService,
        IPlatformService platformService,
        string engineId,
        string buildId,
        CancellationToken cancellationToken = default
    )
    {
        IDistributedReaderWriterLock @lock = await lockFactory.CreateAsync(engineId, cancellationToken);
        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            if (!await buildJobService.BuildJobStartedAsync(engineId, buildId, cancellationToken))
                return false;
        }
        await platformService.BuildStartedAsync(buildId, CancellationToken.None);

        await UpdateTrainJobStatus(platformService, buildId, new ProgressStatus(0), 0, cancellationToken);
        _logger.LogInformation("Build started ({BuildId})", buildId);
        return true;
    }

    private async Task<bool> TrainJobCompletedAsync(
        IDistributedReaderWriterLockFactory lockFactory,
        IBuildJobService buildJobService,
        string engineId,
        string buildId,
        int corpusSize,
        double confidence,
        string? buildOptions,
        CancellationToken cancellationToken
    )
    {
        try
        {
            IDistributedReaderWriterLock @lock = await lockFactory.CreateAsync(engineId, cancellationToken);
            await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                return await buildJobService.StartBuildJobAsync(
                    BuildJobRunnerType.Hangfire,
                    engineId,
                    buildId,
                    BuildStage.Postprocess,
                    (corpusSize, confidence),
                    buildOptions,
                    cancellationToken
                );
            }
        }
        finally
        {
            _curBuildStatus.Remove(buildId);
        }
    }

    private async Task TrainJobFaultedAsync(
        IDistributedReaderWriterLockFactory lockFactory,
        IBuildJobService buildJobService,
        IPlatformService platformService,
        string engineId,
        string buildId,
        string message,
        CancellationToken cancellationToken
    )
    {
        try
        {
            IDistributedReaderWriterLock @lock = await lockFactory.CreateAsync(engineId, cancellationToken);
            await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                await platformService.BuildFaultedAsync(buildId, message, cancellationToken);
                await buildJobService.BuildJobFinishedAsync(
                    engineId,
                    buildId,
                    buildComplete: false,
                    CancellationToken.None
                );
            }
            _logger.LogError("Build faulted ({BuildId}). Error: {ErrorMessage}", buildId, message);
        }
        finally
        {
            _curBuildStatus.Remove(buildId);
        }
    }

    private async Task TrainJobCanceledAsync(
        IDistributedReaderWriterLockFactory lockFactory,
        IBuildJobService buildJobService,
        IPlatformService platformService,
        string engineId,
        string buildId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            IDistributedReaderWriterLock @lock = await lockFactory.CreateAsync(engineId, cancellationToken);
            await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                await platformService.BuildCanceledAsync(buildId, cancellationToken);
                await buildJobService.BuildJobFinishedAsync(
                    engineId,
                    buildId,
                    buildComplete: false,
                    CancellationToken.None
                );
            }
            _logger.LogInformation("Build canceled ({BuildId})", buildId);
        }
        finally
        {
            try
            {
                await _sharedFileService.DeleteAsync($"builds/{buildId}/", CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to to delete job data for build {BuildId}.", buildId);
            }
            _curBuildStatus.Remove(buildId);
        }
    }

    private async Task UpdateTrainJobStatus(
        IPlatformService platformService,
        string buildId,
        ProgressStatus progressStatus,
        int? queueDepth = null,
        CancellationToken cancellationToken = default
    )
    {
        if (
            _curBuildStatus.TryGetValue(buildId, out ProgressStatus curProgressStatus)
            && curProgressStatus.Equals(progressStatus)
        )
        {
            return;
        }
        await platformService.UpdateBuildStatusAsync(buildId, progressStatus, queueDepth, cancellationToken);
        _curBuildStatus[buildId] = progressStatus;
    }

    private static double GetMetric(ClearMLTask task, string metric, string variant)
    {
        if (!task.LastMetrics.TryGetValue(metric, out IReadOnlyDictionary<string, ClearMLMetricsEvent>? metricVariants))
            return 0;

        if (!metricVariants.TryGetValue(variant, out ClearMLMetricsEvent? metricEvent))
            return 0;

        return metricEvent.Value;
    }

    private static string CreateMD5(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }
}
