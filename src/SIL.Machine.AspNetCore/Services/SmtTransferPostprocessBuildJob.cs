namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferPostprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IDataAccessContext dataAccessContext,
    IBuildJobService buildJobService,
    ILogger<SmtTransferPostprocessBuildJob> logger,
    ISharedFileService sharedFileService,
    IRepository<TrainSegmentPair> trainSegmentPairs,
    ISmtModelFactory smtModelFactory,
    ITruecaserFactory truecaserFactory,
    IOptionsMonitor<SmtTransferEngineOptions> options
)
    : PostprocessBuildJob(
        platformService,
        engines,
        lockFactory,
        dataAccessContext,
        buildJobService,
        logger,
        sharedFileService
    )
{
    private readonly ISmtModelFactory _smtModelFactory = smtModelFactory;
    private readonly ITruecaserFactory _truecaserFactory = truecaserFactory;
    private readonly IRepository<TrainSegmentPair> _trainSegmentPairs = trainSegmentPairs;
    private readonly IOptionsMonitor<SmtTransferEngineOptions> _options = options;

    protected override async Task<int> SaveModelAsync(string engineId, string buildId)
    {
        await using (
            Stream engineStream = await SharedFileService.OpenReadAsync(
                $"builds/{buildId}/model.tar.gz",
                CancellationToken.None
            )
        )
        {
            await _smtModelFactory.UpdateEngineFromAsync(
                Path.Combine(_options.CurrentValue.EnginesDir, engineId),
                engineStream,
                CancellationToken.None
            );
        }
        return await TrainOnNewSegmentPairsAsync(engineId);
    }

    private async Task<int> TrainOnNewSegmentPairsAsync(string engineId)
    {
        IReadOnlyList<TrainSegmentPair> segmentPairs = await _trainSegmentPairs.GetAllAsync(p =>
            p.TranslationEngineRef == engineId
        );
        if (segmentPairs.Count == 0)
            return segmentPairs.Count;

        string engineDir = Path.Combine(_options.CurrentValue.EnginesDir, engineId);
        var tokenizer = new LatinWordTokenizer();
        var detokenizer = new LatinWordDetokenizer();
        ITruecaser truecaser = await _truecaserFactory.CreateAsync(engineDir);
        using IInteractiveTranslationModel smtModel = await _smtModelFactory.CreateAsync(
            engineDir,
            tokenizer,
            detokenizer,
            truecaser
        );
        foreach (TrainSegmentPair segmentPair in segmentPairs)
        {
            await smtModel.TrainSegmentAsync(segmentPair.Source, segmentPair.Target);
        }
        await smtModel.SaveAsync();
        return segmentPairs.Count;
    }
}
