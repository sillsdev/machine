namespace SIL.Machine.AspNetCore.Services;

public class CleanupOldModelsJob(ISharedFileService sharedFileService) : ICleanupOldModelsJob
{
    public ISharedFileService SharedFileService { get; } = sharedFileService;
    private List<string> _filesPreviouslyMarkedForDeletion = [];
    private readonly List<string> _filesNewlyMarkedForDeletion = [];

    public async Task RunAsync()
    {
        var files = await SharedFileService.ListFilesAsync(ISharedFileService.ModelDirectory);
        // split name by underscore into engineID and buildRevision
        Dictionary<string, int> modelsByEngineId = [];
        foreach (string file in files)
        {
            string[] parts = file.Split("_");
            if (parts.Length != 2)
            {
                await DeleteFileAsync(file);
                continue;
            }
            string engineId = parts[0];
            if (!int.TryParse(parts[1], out int buildRevision))
            {
                await DeleteFileAsync(file);
                continue;
            }
            if (!modelsByEngineId.TryGetValue(engineId, out int value))
                modelsByEngineId[engineId] = buildRevision;
            else if (value < buildRevision)
                await DeleteFileAsync(engineId + "_" + value.ToString());
            else
                await DeleteFileAsync(file);
        }
        // roll over the list of files previously marked for deletion
        _filesPreviouslyMarkedForDeletion = new List<string>(_filesNewlyMarkedForDeletion);
        _filesNewlyMarkedForDeletion.Clear();
    }

    private async Task DeleteFileAsync(string filename)
    {
        // If a file has been requested to be deleted twice, delete it. Otherwise, mark it for deletion.
        if (_filesPreviouslyMarkedForDeletion.Contains(filename))
        {
            await SharedFileService.DeleteAsync(filename);
        }
        else
        {
            _filesNewlyMarkedForDeletion.Add(filename);
        }
    }
}
