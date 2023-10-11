namespace SIL.Machine.AspNetCore.Services;

public enum BuildJobType
{
    Cpu,
    Gpu
}

public interface IBuildJobService
{
    Task<IReadOnlyList<TranslationEngine>> GetBuildingEnginesAsync(
        BuildJobRunner runner,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsEngineBuilding(string engineId, CancellationToken cancellationToken = default);

    Task CreateEngineAsync(
        IEnumerable<BuildJobType> jobTypes,
        string engineId,
        string? name = null,
        CancellationToken cancellationToken = default
    );

    Task DeleteEngineAsync(
        IEnumerable<BuildJobType> jobTypes,
        string engineId,
        CancellationToken cancellationToken = default
    );

    Task<bool> StartBuildJobAsync(
        BuildJobType jobType,
        TranslationEngineType engineType,
        string engineId,
        string buildId,
        string stage,
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
