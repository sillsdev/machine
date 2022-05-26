namespace SIL.Machine.WebApi.Services;

public interface IClearMLService
{
    Task<string> CreateProjectAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default
    );
    Task<bool> DeleteProjectAsync(string id, CancellationToken cancellationToken = default);
    Task<string?> GetProjectIdAsync(string name, CancellationToken cancellationToken = default);

    Task<string> CreateTaskAsync(
        string name,
        string projectId,
        string sourceLanguageTag,
        string targetLanguageTag,
        Uri buildUri,
        CancellationToken cancellationToken = default
    );
    Task<bool> EnqueueTaskAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> DequeueTaskAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> StopTaskAsync(string id, CancellationToken cancellationToken = default);
    Task<ClearMLTask?> GetTaskAsync(string name, string projectId, CancellationToken cancellationToken = default);
    Task<ClearMLTask?> GetTaskAsync(string id, CancellationToken cancellationToken = default);
}
