using static SIL.Machine.AspNetCore.Services.HangfireBuildJobRunner;

namespace SIL.Machine.AspNetCore.Services;

public class NmtHangfireBuildJobFactory : IHangfireBuildJobFactory
{
    public TranslationEngineType EngineType => TranslationEngineType.Nmt;

    public Job CreateJob(string engineId, string buildId, string stage, object? data)
    {
        return stage switch
        {
            NmtBuildStages.Preprocess
                => CreateJob<NmtPreprocessBuildJob, IReadOnlyList<Corpus>>(engineId, buildId, "nmt", data),
            NmtBuildStages.Postprocess
                => CreateJob<NmtPostprocessBuildJob, (int, double)>(engineId, buildId, "nmt", data),
            NmtBuildStages.Train => CreateJob<NmtTrainBuildJob>(engineId, buildId, "nmt"),
            _ => throw new ArgumentException("Unknown build stage.", nameof(stage)),
        };
    }
}
