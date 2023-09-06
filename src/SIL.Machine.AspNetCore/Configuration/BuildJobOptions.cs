namespace SIL.Machine.AspNetCore.Configuration;

public class BuildJobOptions
{
    public const string Key = "BuildJob";

    public Dictionary<BuildJobType, BuildJobRunner> Runners { get; set; } =
        new() { { BuildJobType.Cpu, BuildJobRunner.Hangfire }, { BuildJobType.Gpu, BuildJobRunner.ClearML } };
}
