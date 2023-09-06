namespace SIL.Machine.AspNetCore.Services;

public class ClearMLMonitorService : RecurrentTask
{
    private static readonly string EvalMetric = CreateMD5("eval");
    private static readonly string BleuVariant = CreateMD5("bleu");

    private static readonly string SummaryMetric = CreateMD5("Summary");
    private static readonly string CorpusSizeVariant = CreateMD5("corpus_size");

    private readonly IClearMLService _clearMLService;
    private readonly ISharedFileService _sharedFileService;
    private readonly ILogger<ClearMLMonitorService> _logger;
    private readonly Dictionary<string, ProgressStatus> _curBuildStatus = new();

    public ClearMLMonitorService(
        IServiceProvider services,
        IClearMLService clearMLService,
        ISharedFileService sharedFileService,
        IOptions<ClearMLOptions> options,
        ILogger<ClearMLMonitorService> logger
    )
        : base(
            "ClearML monitor service",
            services,
            options.Value.BuildPollingTimeout,
            logger,
            options.Value.BuildPollingEnabled
        )
    {
        _clearMLService = clearMLService;
        _sharedFileService = sharedFileService;
        _logger = logger;
    }

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var buildJobService = scope.ServiceProvider.GetRequiredService<IBuildJobService>();
            IReadOnlyList<TranslationEngine> trainingEngines = await buildJobService.GetBuildingEnginesAsync(
                BuildJobRunner.ClearML,
                cancellationToken
            );
            if (trainingEngines.Count == 0)
                return;

            Dictionary<string, ClearMLTask> tasks = (
                await _clearMLService.GetTasksByIdAsync(
                    trainingEngines.Select(e => e.CurrentBuild!.JobId),
                    cancellationToken
                )
            ).ToDictionary(t => t.Id);

            var platformService = scope.ServiceProvider.GetRequiredService<IPlatformService>();
            var lockFactory = scope.ServiceProvider.GetRequiredService<IDistributedReaderWriterLockFactory>();
            foreach (TranslationEngine engine in trainingEngines)
            {
                if (engine.CurrentBuild is null || !tasks.TryGetValue(engine.CurrentBuild.JobId, out ClearMLTask? task))
                    continue;

                if (engine.CurrentBuild.Stage == NmtBuildStages.Train)
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
                            await UpdateTrainJobStatus(
                                platformService,
                                engine.CurrentBuild.BuildId,
                                new ProgressStatus(task.LastIteration),
                                cancellationToken
                            );
                            break;

                        case ClearMLTaskStatus.Completed:
                            await UpdateTrainJobStatus(
                                platformService,
                                engine.CurrentBuild.BuildId,
                                new ProgressStatus(task.LastIteration),
                                cancellationToken
                            );
                            bool canceling = !await TrainJobCompletedAsync(
                                lockFactory,
                                buildJobService,
                                engine.EngineId,
                                engine.CurrentBuild.BuildId,
                                (int)GetMetric(task, SummaryMetric, CorpusSizeVariant),
                                GetMetric(task, EvalMetric, BleuVariant),
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

                        case ClearMLTaskStatus.Stopped:
                            await TrainJobCanceledAsync(
                                lockFactory,
                                buildJobService,
                                platformService,
                                engine.EngineId,
                                engine.CurrentBuild.BuildId,
                                cancellationToken
                            );
                            break;

                        case ClearMLTaskStatus.Failed:
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
        _logger.LogInformation("Build started ({0})", buildId);
        return true;
    }

    private async Task<bool> TrainJobCompletedAsync(
        IDistributedReaderWriterLockFactory lockFactory,
        IBuildJobService buildJobService,
        string engineId,
        string buildId,
        int corpusSize,
        double confidence,
        CancellationToken cancellationToken
    )
    {
        try
        {
            IDistributedReaderWriterLock @lock = await lockFactory.CreateAsync(engineId, cancellationToken);
            await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
            {
                return await buildJobService.StartBuildJobAsync(
                    BuildJobType.Cpu,
                    TranslationEngineType.Nmt,
                    engineId,
                    buildId,
                    NmtBuildStages.Postprocess,
                    (corpusSize, confidence),
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
            _logger.LogError("Build faulted ({0}). Error: {1}", buildId, message);
        }
        finally
        {
            try
            {
                await _sharedFileService.DeleteAsync($"builds/{buildId}/", CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to to delete job data for build {0}.", buildId);
            }
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
            _logger.LogInformation("Build canceled ({0})", buildId);
        }
        finally
        {
            try
            {
                await _sharedFileService.DeleteAsync($"builds/{buildId}/", CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to to delete job data for build {0}.", buildId);
            }
            _curBuildStatus.Remove(buildId);
        }
    }

    private async Task UpdateTrainJobStatus(
        IPlatformService platformService,
        string buildId,
        ProgressStatus progressStatus,
        CancellationToken cancellationToken
    )
    {
        if (
            _curBuildStatus.TryGetValue(buildId, out ProgressStatus curProgressStatus)
            && curProgressStatus.Equals(progressStatus)
        )
        {
            return;
        }
        await platformService.UpdateBuildStatusAsync(buildId, progressStatus, cancellationToken);
        _curBuildStatus[buildId] = progressStatus;
    }

    private static double GetMetric(ClearMLTask task, string metric, string variant)
    {
        if (!task.LastMetrics.TryGetValue(metric, out Dictionary<string, ClearMLMetricsEvent>? metricVariants))
            return 0;

        if (!metricVariants.TryGetValue(variant, out ClearMLMetricsEvent? metricEvent))
            return 0;

        return metricEvent.Value;
    }

    private static string CreateMD5(string input)
    {
        using var md5 = MD5.Create();

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes);
    }
}
