using static SIL.Machine.AspNetCore.Services.HangfireBuildJobRunner;

namespace SIL.Machine.AspNetCore.Services;

public class SmtTransferHangfireBuildJobFactory : IHangfireBuildJobFactory
{
    public TranslationEngineType EngineType => TranslationEngineType.SmtTransfer;

    public Job CreateJob(string engineId, string buildId, string stage, object? data, string? buildOptions)
    {
        return stage switch
        {
            SmtTransferBuildStages.Train
                => CreateJob<SmtTransferBuildJob, IReadOnlyList<Corpus>>(
                    engineId,
                    buildId,
                    "smt_transfer",
                    data,
                    buildOptions
                ),
            _ => throw new ArgumentException("Unknown build stage.", nameof(stage)),
        };
    }
}
