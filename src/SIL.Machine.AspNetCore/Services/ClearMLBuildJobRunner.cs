namespace SIL.Machine.AspNetCore.Services;

public class ClearMLBuildJobRunner(
    IClearMLService clearMLService,
    IEnumerable<IClearMLBuildJobFactory> buildJobFactories
) : IBuildJobRunner
{
    private readonly IClearMLService _clearMLService = clearMLService;
    private readonly Dictionary<TranslationEngineType, IClearMLBuildJobFactory> _buildJobFactories =
        buildJobFactories.ToDictionary(f => f.EngineType);

    public BuildJobRunner Type => BuildJobRunner.ClearML;

    public async Task CreateEngineAsync(
        string engineId,
        string? name = null,
        CancellationToken cancellationToken = default
    )
    {
        await _clearMLService.CreateProjectAsync(engineId, name, cancellationToken);
    }

    public async Task DeleteEngineAsync(string engineId, CancellationToken cancellationToken = default)
    {
        string? projectId = await _clearMLService.GetProjectIdAsync(engineId, cancellationToken);
        if (projectId is not null)
            await _clearMLService.DeleteProjectAsync(projectId, cancellationToken);
    }

    public async Task<string> CreateJobAsync(
        TranslationEngineType engineType,
        string engineId,
        string buildId,
        string stage,
        object? data = null,
        string? buildOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        string? projectId = await _clearMLService.GetProjectIdAsync(engineId, cancellationToken);
        projectId ??= await _clearMLService.CreateProjectAsync(engineId, cancellationToken: cancellationToken);

        ClearMLTask? task = await _clearMLService.GetTaskByNameAsync(buildId, cancellationToken);
        if (task is not null)
            return task.Id;

        IClearMLBuildJobFactory buildJobFactory = _buildJobFactories[engineType];
        string script = await buildJobFactory.CreateJobScriptAsync(
            engineId,
            buildId,
            stage,
            data,
            buildOptions,
            cancellationToken
        );
        return await _clearMLService.CreateTaskAsync(buildId, projectId, script, cancellationToken);
    }

    public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return _clearMLService.DeleteTaskAsync(jobId, cancellationToken);
    }

    public Task<bool> EnqueueJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return _clearMLService.EnqueueTaskAsync(jobId, cancellationToken);
    }

    public Task<bool> StopJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return _clearMLService.StopTaskAsync(jobId, cancellationToken);
    }
}
