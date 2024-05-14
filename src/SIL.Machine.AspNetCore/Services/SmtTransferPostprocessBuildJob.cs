namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferPostprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IOptionsMonitor<SmtTransferEngineOptions> engineOptions,
    IDistributedReaderWriterLockFactory lockFactory,
    IBuildJobService buildJobService,
    ILogger<SmtTransferPostprocessBuildJob> logger,
    ISharedFileService sharedFileService,
    IRepository<TrainSegmentPair> trainSegmentPairs,
    SmtTransferEngineStateService stateService
) : PostprocessBuildJob(platformService, engines, lockFactory, buildJobService, logger, sharedFileService)
{
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _engineOptions = engineOptions;

    private readonly SmtTransferEngineStateService _stateService = stateService;
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs = trainSegmentPairs;

    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        (int, double) data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using (await @lock.WriterLockAsync(cancellationToken: CancellationToken.None))
        {
            await DownloadBuiltEngineAsync(engineId, cancellationToken);
            int segmentPairsSize = await TrainOnNewSegmentPairs(engineId, @lock, cancellationToken);
            await PlatformService.BuildCompletedAsync(
                buildId,
                trainSize: data.Item1 + segmentPairsSize,
                confidence: Math.Round(data.Item2, 2, MidpointRounding.AwayFromZero),
                cancellationToken: CancellationToken.None
            );
            await BuildJobService.BuildJobFinishedAsync(engineId, buildId, buildComplete: true, CancellationToken.None);
        }

        Logger.LogInformation("Build completed ({0}).", buildId);
    }

    private async Task DownloadBuiltEngineAsync(string engineId, CancellationToken cancellationToken)
    {
        // extract SMT engine locally
        string sharedFilePath = $"models/{engineId}.zip";
        using Stream sharedStream = await SharedFileService.OpenReadAsync(sharedFilePath, cancellationToken);
        string extractDir = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId);
        Directory.CreateDirectory(extractDir);
        ZipFile.ExtractToDirectory(sharedStream, extractDir, overwriteFiles: true);
        await SharedFileService.DeleteAsync(sharedFilePath);
    }

    private async Task<int> TrainOnNewSegmentPairs(
        string engineId,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
        if (engine is null)
            throw new OperationCanceledException();

        await using (await @lock.WriterLockAsync(cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<TrainSegmentPair> segmentPairs = await _trainSegmentPairs.GetAllAsync(
                p => p.TranslationEngineRef == engine.Id,
                CancellationToken.None
            );
            if (segmentPairs.Count == 0)
                return segmentPairs.Count;

            SmtTransferEngineState state = _stateService.Get(engineId);
            using HybridTranslationEngine hybridEngine = await state.GetHybridEngineAsync(engine.BuildRevision);
            {
                foreach (TrainSegmentPair segmentPair in segmentPairs)
                {
                    await hybridEngine.TrainSegmentAsync(
                        segmentPair.Source,
                        segmentPair.Target,
                        cancellationToken: CancellationToken.None
                    );
                }
            }
            return segmentPairs.Count;
        }
    }
}
