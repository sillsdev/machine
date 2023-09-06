namespace SIL.Machine.AspNetCore.Services;

public interface IBuildJobRunner
{
    BuildJobRunner Type { get; }

    Task CreateEngineAsync(string engineId, string? name = null, CancellationToken cancellationToken = default);
    Task DeleteEngineAsync(string engineId, CancellationToken cancellationToken = default);

    Task<string> CreateJobAsync(
        TranslationEngineType engineType,
        string engineId,
        string buildId,
        string stage,
        object? data = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<bool> EnqueueJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<bool> StopJobAsync(string jobId, CancellationToken cancellationToken = default);
}
