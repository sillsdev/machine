namespace SIL.Machine.AspNetCore.Services;

public class PostprocessBuildJob(
    IPlatformService platformService,
    IRepository<TranslationEngine> engines,
    IDistributedReaderWriterLockFactory lockFactory,
    IDataAccessContext dataAccessContext,
    IBuildJobService buildJobService,
    ILogger<PostprocessBuildJob> logger,
    ISharedFileService sharedFileService
) : HangfireBuildJob<(int, double)>(platformService, engines, lockFactory, dataAccessContext, buildJobService, logger)
{
    protected ISharedFileService SharedFileService { get; } = sharedFileService;

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

        await using (
            Stream pretranslationsStream = await SharedFileService.OpenReadAsync(
                $"builds/{buildId}/pretranslate.trg.json",
                cancellationToken
            )
        )
        {
            await PlatformService.InsertPretranslationsAsync(engineId, pretranslationsStream, cancellationToken);
        }

        await using (await @lock.WriterLockAsync(cancellationToken: CancellationToken.None))
        {
            await DataAccessContext.WithTransactionAsync(
                async (ct) =>
                {
                    int additionalCorpusSize = await SaveModelAsync(engineId, buildId);
                    await PlatformService.BuildCompletedAsync(
                        buildId,
                        corpusSize + additionalCorpusSize,
                        Math.Round(confidence, 2, MidpointRounding.AwayFromZero),
                        CancellationToken.None
                    );
                    await BuildJobService.BuildJobFinishedAsync(
                        engineId,
                        buildId,
                        buildComplete: true,
                        CancellationToken.None
                    );
                },
                cancellationToken: CancellationToken.None
            );
        }

        Logger.LogInformation("Build completed ({0}).", buildId);
    }

    protected virtual Task<int> SaveModelAsync(string engineId, string buildId)
    {
        return Task.FromResult(0);
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
            if (completionStatus is not JobCompletionStatus.Faulted)
                await SharedFileService.DeleteAsync($"builds/{buildId}/");
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Unable to to delete job data for build {0}.", buildId);
        }
    }
}
