namespace SIL.Machine.AspNetCore.Services;

public class NmtPostprocessBuildJob : HangfireBuildJob<(int, double)>
{
    private readonly ISharedFileService _sharedFileService;

    public NmtPostprocessBuildJob(
        IPlatformService platformService,
        IRepository<TranslationEngine> engines,
        IDistributedReaderWriterLockFactory lockFactory,
        IBuildJobService buildJobService,
        ILogger<NmtPostprocessBuildJob> logger,
        ISharedFileService sharedFileService
    )
        : base(platformService, engines, lockFactory, buildJobService, logger)
    {
        _sharedFileService = sharedFileService;
    }

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

    protected override async Task CleanupAsync(
        string engineId,
        string buildId,
        (int, double) data,
        IDistributedReaderWriterLock @lock,
        JobCompletionStatus completionStatus
    )
    {
        if (completionStatus is JobCompletionStatus.Restarting)
            return;

        try
        {
            await _sharedFileService.DeleteAsync($"builds/{buildId}/");
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Unable to to delete job data for build {0}.", buildId);
        }
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

        await PlatformService.InsertPretranslationsAsync(engineId, pretranslations, cancellationToken);
    }
}
