namespace SIL.Machine.AspNetCore.Services;

public interface IBuildJobService
{
    Task<IReadOnlyList<TranslationEngine>> GetBuildingEnginesAsync(
        BuildJobRunnerType runner,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsEngineBuilding(string engineId, CancellationToken cancellationToken = default);

    Task CreateEngineAsync(string engineId, string? name = null, CancellationToken cancellationToken = default);

    Task DeleteEngineAsync(string engineId, CancellationToken cancellationToken = default);

    Task<bool> StartBuildJobAsync(
        BuildJobRunnerType jobType,
        string engineId,
        string buildId,
        BuildStage stage,
        object? data = default,
        string? buildOptions = default,
        CancellationToken cancellationToken = default
    );

    Task<(string? BuildId, BuildJobState State)> CancelBuildJobAsync(
        string engineId,
        CancellationToken cancellationToken = default
    );

    Task<bool> BuildJobStartedAsync(string engineId, string buildId, CancellationToken cancellationToken = default);

    Task BuildJobFinishedAsync(
        string engineId,
        string buildId,
        bool buildComplete,
        CancellationToken cancellationToken = default
    );

    Task BuildJobRestartingAsync(string engineId, string buildId, CancellationToken cancellationToken = default);
}
