namespace SIL.Machine.AspNetCore.Services;

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
        string buildId,
        string projectId,
        string script,
        string dockerImage,
        CancellationToken cancellationToken = default
    );
    Task<bool> DeleteTaskAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> EnqueueTaskAsync(string id, string queue, CancellationToken cancellationToken = default);
    Task<bool> DequeueTaskAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> StopTaskAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClearMLTask>> GetTasksForQueueAsync(string queue, CancellationToken cancellationToken = default);
    Task<ClearMLTask?> GetTaskByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClearMLTask>> GetTasksByIdAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default
    );
}
