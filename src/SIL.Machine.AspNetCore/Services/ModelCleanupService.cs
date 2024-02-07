namespace SIL.Machine.AspNetCore.Services;

public class ModelCleanupService(
    IServiceProvider services,
    ISharedFileService sharedFileService,
    IRepository<TranslationEngine> engines,
    ILogger<ModelCleanupService> logger
) : RecurrentTask("Model Cleanup Service", services, RefreshPeriod, logger)
{
    private ISharedFileService _sharedFileService = sharedFileService;
    private ILogger<ModelCleanupService> _logger = logger;
    private IRepository<TranslationEngine> _engines = engines;
    private List<string> _filesPreviouslyMarkedForDeletion = [];
    private readonly List<string> _filesNewlyMarkedForDeletion = [];
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
        // Get all engine ids from the database
        IReadOnlyList<TranslationEngine>? allEngines = await _engines.GetAllAsync(cancellationToken: cancellationToken);
        HashSet<string> allValidFilenames = allEngines
            .Select(e => NmtEngineService.GetModelPath(e.EngineId, e.BuildRevision))
            .ToHashSet();

        foreach (string path in paths)
        {
            if (!allValidFilenames.Contains(path))
            {
                await DeleteFileAsync(
                    path,
                    $"file in S3 bucket not found in database.  It may be an old rev, etc.",
                    cancellationToken
                );
            }
        }
        // roll over the list of files previously marked for deletion
        _filesPreviouslyMarkedForDeletion = new List<string>(_filesNewlyMarkedForDeletion);
        _filesNewlyMarkedForDeletion.Clear();
    }

    private async Task DeleteFileAsync(string path, string message, CancellationToken cancellationToken = default)
    {
        // If a file has been requested to be deleted twice, delete it. Otherwise, mark it for deletion.
        if (_filesPreviouslyMarkedForDeletion.Contains(path))
        {
            _logger.LogInformation("Deleting old model file {filename}: {message}", path, message);
            await _sharedFileService.DeleteAsync(path, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Marking old model file {filename} for deletion: {message}", path, message);
            _filesNewlyMarkedForDeletion.Add(path);
        }
    }
}
