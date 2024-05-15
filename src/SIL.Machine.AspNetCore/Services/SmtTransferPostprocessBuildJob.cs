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

    public override bool GetPretranslationEnabled() => false;

    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        (int, double) data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        int corpusSize = data.Item1;
        await DownloadBuiltEngineAsync(engineId, cancellationToken);

        int segmentPairsSize = await TrainOnNewSegmentPairs(engineId, @lock, cancellationToken);

        await base.DoWorkAsync(
            engineId,
            buildId,
            (corpusSize + segmentPairsSize, data.Item2),
            buildOptions,
            @lock,
            cancellationToken
        );
    }

    private async Task DownloadBuiltEngineAsync(string engineId, CancellationToken cancellationToken)
    {
        // copy SMT engine locally
        string sharedFilePath = $"models/{engineId}.tar.gz";
        string localZipPath = Path.GetTempFileName();
        using Stream sharedStream = await SharedFileService.OpenReadAsync(sharedFilePath, cancellationToken);
        using FileStream fileStream = File.Create(localZipPath);
        await sharedStream.CopyToAsync(fileStream, cancellationToken);
        fileStream.Close();

        // extract files from SMT engine
        string extractDir = Path.Combine(_engineOptions.CurrentValue.EnginesDir, engineId);
        Directory.CreateDirectory(extractDir);
        using ZipArchive archive = ZipFile.OpenRead(localZipPath);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string entryPath = Path.Combine(extractDir, entry.FullName);
            // create intermediate directories if necessary
            string? parentDirectory = Path.GetDirectoryName(entryPath);
            if (parentDirectory != null)
                Directory.CreateDirectory(parentDirectory);
            entry.ExtractToFile(entryPath, overwrite: true);
        }

        // delete file from S3 bucket
        await SharedFileService.DeleteAsync(sharedFilePath);
        // delete local zip file
        File.Delete(localZipPath);
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
