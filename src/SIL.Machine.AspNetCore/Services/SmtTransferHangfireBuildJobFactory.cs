using static SIL.Machine.AspNetCore.Services.HangfireBuildJobRunner;

namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferHangfireBuildJobFactory : IHangfireBuildJobFactory
{
    public TranslationEngineType EngineType => TranslationEngineType.SmtTransfer;

    public Job CreateJob(string engineId, string buildId, BuildStage stage, object? data, string? buildOptions)
    {
        return stage switch
        {
            BuildStage.Preprocess
                => CreateJob<PreprocessBuildJob, IReadOnlyList<Corpus>>(
                    engineId,
                    buildId,
                    "smt_transfer",
                    data,
                    buildOptions
                ),
            BuildStage.Postprocess
                => CreateJob<SmtTransferPostprocessBuildJob, (int, double)>(
                    engineId,
                    buildId,
                    "smt_transfer",
                    data,
                    buildOptions
                ),
            BuildStage.Train => CreateJob<SmtTransferTrainBuildJob>(engineId, buildId, "smt_transfer", buildOptions),
            _ => throw new ArgumentException("Unknown build stage.", nameof(stage)),
        };
    }
}
