namespace SIL.Machine.AspNetCore.Services;

public interface IHangfireBuildJobFactory
{
    TranslationEngineType EngineType { get; }

    Job CreateJob(string engineId, string buildId, string stage, object? data);
}
