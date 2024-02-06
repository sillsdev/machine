namespace SIL.Machine.AspNetCore.Services;

public class CleanupOldModelsService(
    IServiceProvider services,
    ISharedFileService sharedFileService,
    IRepository<TranslationEngine> engines,
    ILogger<CleanupOldModelsService> logger
) : RecurrentTask("Cleanup Old Models Service", services, RefreshPeriod, logger)
{
    public ISharedFileService SharedFileService { get; } = sharedFileService;
    private ILogger<CleanupOldModelsService> _logger = logger;
    private IRepository<TranslationEngine> _engines = engines;
    private List<string> _filesPreviouslyMarkedForDeletion = [];
    private readonly List<string> _filesNewlyMarkedForDeletion = [];
    private static readonly TimeSpan RefreshPeriod = TimeSpan.FromDays(1);

    protected override async Task DoWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        await CheckModelsAsync();
    }

    public async Task CheckModelsAsync()
    {
        _logger.LogInformation("Running model cleanup job");
        var paths = await SharedFileService.ListFilesAsync(ISharedFileService.ModelDirectory);
        foreach (string path in paths)
        {
            var filename = Path.GetFileName(path);
            var filenameWithoutExtensions = filename.Split(".")[0];
            var extension = filename[^(filename.Length - filenameWithoutExtensions.Length)..];
            if (extension != ".tar.gz")
            {
                await DeleteFileAsync(path, $"filename has to have .tar.gz extension, instead has {extension}");
                continue;
            }
            string[] parts = filenameWithoutExtensions.Split("_");
            if (parts.Length != 2)
            {
                await DeleteFileAsync(path, $"filename has to have one underscore, instead has {parts.Length - 1}");
                continue;
            }
            string engineId = parts[0];
            TranslationEngine? engine = await _engines.GetAsync(e => e.EngineId == engineId);
            if (engine is null)
            {
                await DeleteFileAsync(path, $"engine {engineId} does not exist in the database.");
                continue;
            }
            if (!int.TryParse(parts[1], out int parsedBuildRevision))
            {
                await DeleteFileAsync(path, $"cannot parse build revision from {parts[1]} for engine {engineId}");
                continue;
            }
            if (engine.BuildRevision > parsedBuildRevision)
                await DeleteFileAsync(
                    path,
                    $"build revision {parsedBuildRevision} is older than the current build revision {engine.BuildRevision}"
                );
        }
        // roll over the list of files previously marked for deletion
        _filesPreviouslyMarkedForDeletion = new List<string>(_filesNewlyMarkedForDeletion);
        _filesNewlyMarkedForDeletion.Clear();
    }

    private async Task DeleteFileAsync(string filename, string message)
    {
        // If a file has been requested to be deleted twice, delete it. Otherwise, mark it for deletion.
        if (_filesPreviouslyMarkedForDeletion.Contains(filename))
        {
            _logger.LogInformation("Deleting old model file {filename}: {message}", filename, message);
            await SharedFileService.DeleteAsync(filename);
        }
        else
        {
            _logger.LogInformation("Marking old model file {filename} for deletion: {message}", filename, message);
            _filesNewlyMarkedForDeletion.Add(filename);
        }
    }
}
