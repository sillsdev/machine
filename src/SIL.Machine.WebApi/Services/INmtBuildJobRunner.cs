namespace SIL.Machine.WebApi.Services;

public interface INmtBuildJobRunner
{
    Task RunAsync(string engineId, string buildId, CancellationToken cancellationToken = default);
}
