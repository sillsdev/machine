namespace SIL.Machine.AspNetCore.Services;

public class ModelCleanupService(
    IServiceProvider services,
    ISharedFileService sharedFileService,
    IRepository<TranslationEngine> engines,
    ILogger<ModelCleanupService> logger
) : RecurrentTask("Model Cleanup Service", services, RefreshPeriod, logger)
{
    private ISharedFileService SharedFileService { get; } = sharedFileService;
    private ILogger<ModelCleanupService> _logger = logger;
    private IRepository<TranslationEngine> _engines = engines;
    private List<string> _filesPreviouslyMarkedForDeletion = [];
    private readonly List<string> _filesNewlyMarkedForDeletion = [];
    private static readonly TimeSpan RefreshPeriod = TimeSpan.FromSeconds(10);

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        await CheckModelsAsync(cancellationToken);
    }

    private async Task CheckModelsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running model cleanup job");
        IReadOnlyCollection<string> paths = await SharedFileService.ListFilesAsync(
            NmtEngineService.ModelDirectory,
            cancellationToken: cancellationToken
        );
        // Get all engine ids from the database
        Dictionary<string, int> engineIdsToRevision = _engines
            .GetAllAsync(cancellationToken: cancellationToken)
            .Result.Select(e => (e.EngineId, e.BuildRevision))
            .ToDictionary<string, int>();

        foreach (string path in paths)
        {
            string filename = Path.GetFileName(path);
            string filenameWithoutExtensions = filename.Split(".")[0];
            string extension = filename[^(filename.Length - filenameWithoutExtensions.Length)..];
            if (extension != ".tar.gz")
            {
                await DeleteFileAsync(
                    path,
                    $"filename has to have .tar.gz extension, instead has {extension}",
                    cancellationToken
                );
                continue;
            }
            string[] parts = filenameWithoutExtensions.Split("_");
            if (parts.Length != 2)
            {
                await DeleteFileAsync(
                    path,
                    $"filename has to have one underscore, instead has {parts.Length - 1}",
                    cancellationToken
                );
                continue;
            }
            string engineId = parts[0];
            if (!engineIdsToRevision.ContainsKey(engineId))
            {
                await DeleteFileAsync(path, $"engine {engineId} does not exist in the database.", cancellationToken);
                continue;
            }
            if (!int.TryParse(parts[1], out int parsedBuildRevision))
            {
                await DeleteFileAsync(
                    path,
                    $"cannot parse build revision from {parts[1]} for engine {engineId}",
                    cancellationToken
                );
                continue;
            }
            if (engineIdsToRevision[engineId] > parsedBuildRevision)
                await DeleteFileAsync(
                    path,
                    $"build revision {parsedBuildRevision} is older than the current build revision {engineIdsToRevision[engineId]}",
                    cancellationToken
                );
        }
        // roll over the list of files previously marked for deletion
        _filesPreviouslyMarkedForDeletion = new List<string>(_filesNewlyMarkedForDeletion);
        _filesNewlyMarkedForDeletion.Clear();
    }

    private async Task DeleteFileAsync(string filename, string message, CancellationToken cancellationToken = default)
    {
        // If a file has been requested to be deleted twice, delete it. Otherwise, mark it for deletion.
        if (_filesPreviouslyMarkedForDeletion.Contains(filename))
        {
            _logger.LogInformation("Deleting old model file {filename}: {message}", filename, message);
            await SharedFileService.DeleteAsync(filename, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Marking old model file {filename} for deletion: {message}", filename, message);
            _filesNewlyMarkedForDeletion.Add(filename);
        }
    }
}
