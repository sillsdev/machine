namespace SIL.Machine.AspNetCore.Services;

public class NmtPostprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IBuildJobService buildJobService,
    ILogger<NmtPostprocessBuildJob> logger,
    ISharedFileService sharedFileService
) : PostprocessBuildJob(platformService, engines, lockFactory, buildJobService, logger, sharedFileService)
{
    protected override async Task DoWorkAsync(
        string engineId,
        string buildId,
        (int, double) data,
        string? buildOptions,
        IDistributedReaderWriterLock @lock,
        CancellationToken cancellationToken
    )
    {
        (int corpusSize, double confidence) = data;

        // The NMT job has successfully completed, so insert the generated pretranslations into the database.
        await InsertPretranslationsAsync(engineId, buildId, cancellationToken);

        await using (await @lock.WriterLockAsync(cancellationToken: CancellationToken.None))
        {
            await PlatformService.BuildCompletedAsync(
                buildId,
                corpusSize,
                Math.Round(confidence, 2, MidpointRounding.AwayFromZero),
                CancellationToken.None
            );
            await BuildJobService.BuildJobFinishedAsync(engineId, buildId, buildComplete: true, CancellationToken.None);
        }

        Logger.LogInformation("Build completed ({0}).", buildId);
    }
}
