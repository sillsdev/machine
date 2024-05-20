namespace SIL.Machine.AspNetCore.Services;

public interface IBuildJobRunner
{
    BuildJobRunnerType Type { get; }

    Task CreateEngineAsync(string engineId, string? name = null, CancellationToken cancellationToken = default);
    Task DeleteEngineAsync(string engineId, CancellationToken cancellationToken = default);

    Task<string> CreateJobAsync(
        TranslationEngineType engineType,
        string engineId,
        string buildId,
        BuildStage stage,
        object? data = null,
        string? buildOptions = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<bool> EnqueueJobAsync(
        string jobId,
        TranslationEngineType engineType,
        CancellationToken cancellationToken = default
    );

    Task<bool> StopJobAsync(string jobId, CancellationToken cancellationToken = default);
}
