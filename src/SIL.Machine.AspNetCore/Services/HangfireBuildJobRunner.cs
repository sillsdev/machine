namespace SIL.Machine.AspNetCore.Services;

public class HangfireBuildJobRunner(
    IBackgroundJobClient jobClient,
    IEnumerable<IHangfireBuildJobFactory> buildJobFactories
) : IBuildJobRunner
{
    public static Job CreateJob<TJob, TData>(
        string engineId,
        string buildId,
        string queue,
        object? data,
        string? buildOptions
    )
        where TJob : HangfireBuildJob<TData>
    {
        ArgumentNullException.ThrowIfNull(data);
        // Token "None" is used here because hangfire injects the proper cancellation token
        return Job.FromExpression<TJob>(
            j => j.RunAsync(engineId, buildId, (TData)data, buildOptions, CancellationToken.None),
            queue
        );
    }

    public static Job CreateJob<TJob>(string engineId, string buildId, string queue, string? buildOptions)
        where TJob : HangfireBuildJob
    {
        // Token "None" is used here because hangfire injects the proper cancellation token
        return Job.FromExpression<TJob>(
            j => j.RunAsync(engineId, buildId, buildOptions, CancellationToken.None),
            queue
        );
    }

    private readonly IBackgroundJobClient _jobClient = jobClient;
    private readonly Dictionary<TranslationEngineType, IHangfireBuildJobFactory> _buildJobFactories =
        buildJobFactories.ToDictionary(f => f.EngineType);

    public BuildJobRunner Type => BuildJobRunner.Hangfire;

    public Task CreateEngineAsync(string engineId, string? name = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteEngineAsync(string engineId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<string> CreateJobAsync(
        TranslationEngineType engineType,
        string engineId,
        string buildId,
        string stage,
        object? data = null,
        string? buildOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        IHangfireBuildJobFactory buildJobFactory = _buildJobFactories[engineType];
        Job job = buildJobFactory.CreateJob(engineId, buildId, stage, data, buildOptions);
        return Task.FromResult(_jobClient.Create(job, new ScheduledState(TimeSpan.FromDays(10000))));
    }

    public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_jobClient.Delete(jobId));
    }

    public Task<bool> EnqueueJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_jobClient.Requeue(jobId));
    }

    public Task<bool> StopJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        // Trigger the cancellation token for the job
        return Task.FromResult(_jobClient.Delete(jobId));
    }
}
