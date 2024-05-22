namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferPostprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IBuildJobService buildJobService,
    ILogger<SmtTransferPostprocessBuildJob> logger,
    ISharedFileService sharedFileService,
    IRepository<TrainSegmentPair> trainSegmentPairs,
    ISmtModelFactory smtModelFactory,
    ITruecaserFactory truecaserFactory
) : PostprocessBuildJob(platformService, engines, lockFactory, buildJobService, logger, sharedFileService)
{
    private readonly ISmtModelFactory _smtModelFactory = smtModelFactory;
    private readonly ITruecaserFactory _truecaserFactory = truecaserFactory;
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
            await _smtModelFactory.DownloadBuiltEngineAsync(engineId, cancellationToken);
            int segmentPairsSize = await TrainOnNewSegmentPairs(engineId, cancellationToken);
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

    private async Task<int> TrainOnNewSegmentPairs(string engineId, CancellationToken cancellationToken)
    {
        TranslationEngine? engine = await Engines.GetAsync(e => e.EngineId == engineId, cancellationToken);
        if (engine is null)
            throw new OperationCanceledException();

        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<TrainSegmentPair> segmentPairs = await _trainSegmentPairs.GetAllAsync(
            p => p.TranslationEngineRef == engine.Id,
            CancellationToken.None
        );
        if (segmentPairs.Count == 0)
            return segmentPairs.Count;

        var tokenizer = new LatinWordTokenizer();
        var detokenizer = new LatinWordDetokenizer();
        ITruecaser truecaser = await _truecaserFactory.CreateAsync(engineId);

        using (
            IInteractiveTranslationModel smtModel = _smtModelFactory.Create(engineId, tokenizer, detokenizer, truecaser)
        )
        {
            foreach (TrainSegmentPair segmentPair in segmentPairs)
            {
                await smtModel.TrainSegmentAsync(
                    segmentPair.Source,
                    segmentPair.Target,
                    cancellationToken: CancellationToken.None
                );
            }
            await smtModel.SaveAsync(CancellationToken.None);
        }
        return segmentPairs.Count;
    }
}
