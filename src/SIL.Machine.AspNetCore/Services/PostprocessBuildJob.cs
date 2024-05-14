namespace SIL.Machine.AspNetCore.Services;

public abstract class PostprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IBuildJobService buildJobService,
    ILogger<PostprocessBuildJob> logger,
    ISharedFileService sharedFileService
) : HangfireBuildJob<(int, double)>(platformService, engines, lockFactory, buildJobService, logger)
{
    protected readonly ISharedFileService SharedFileService = sharedFileService;

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
            if (completionStatus is not JobCompletionStatus.Faulted)
                await SharedFileService.DeleteAsync($"builds/{buildId}/");
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Unable to to delete job data for build {0}.", buildId);
        }
    }

    protected async Task InsertPretranslationsAsync(
        string engineId,
        string buildId,
        CancellationToken cancellationToken
    )
    {
        await using var targetPretranslateStream = await SharedFileService.OpenReadAsync(
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
