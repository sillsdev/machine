namespace SIL.Machine.AspNetCore.Services;

public class ModelCleanupService(
    IServiceProvider services,
    ISharedFileService sharedFileService,
    IRepository<TranslationEngine> engines,
    ILogger<ModelCleanupService> logger
) : RecurrentTask("Model Cleanup Service", services, RefreshPeriod, logger)
{
    private readonly ISharedFileService _sharedFileService = sharedFileService;
    private readonly ILogger<ModelCleanupService> _logger = logger;
    private readonly IRepository<TranslationEngine> _engines = engines;
    private static readonly TimeSpan RefreshPeriod = TimeSpan.FromDays(1);

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        await CheckModelsAsync(cancellationToken);
    }

    private async Task CheckModelsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running model cleanup job");
        IReadOnlyCollection<string> paths = await _sharedFileService.ListFilesAsync(
            NmtEngineService.ModelDirectory,
            cancellationToken: cancellationToken
        );
        // Get all NMT engine ids from the database
        IReadOnlyList<TranslationEngine>? allEngines = await _engines.GetAllAsync(cancellationToken: cancellationToken);
        IEnumerable<string> validNmtFilenames = allEngines
            .Where(e => e.Type == TranslationEngineType.Nmt)
            .Select(e => NmtEngineService.GetModelPath(e.EngineId, e.BuildRevision));
        // If there is a currently running build that creates and pushes a new file, but the database has not
        // updated yet, don't delete the new file.
        IEnumerable<string> validNmtFilenamesForNextBuild = allEngines
            .Where(e => e.Type == TranslationEngineType.Nmt)
            .Select(e => NmtEngineService.GetModelPath(e.EngineId, e.BuildRevision + 1));

        HashSet<string> filenameFilter = validNmtFilenames.Concat(validNmtFilenamesForNextBuild).ToHashSet();

        foreach (string path in paths)
        {
            if (!filenameFilter.Contains(path))
            {
                await DeleteFileAsync(
                    path,
                    $"file in S3 bucket not found in database.  It may be an old rev, etc.",
                    cancellationToken
                );
            }
        }
    }

    private async Task DeleteFileAsync(string path, string message, CancellationToken cancellationToken = default)
    {
        // This may delete a file while it is being downloaded, but the chance is rare
        // enough and the solution easy enough (just download again) to just live with it.
        _logger.LogInformation("Deleting old model file {filename}: {message}", path, message);
        await _sharedFileService.DeleteAsync(path, cancellationToken);
    }
}
