namespace SIL.Machine.AspNetCore.Configuration;

public class BuildJobOptions
{
    public const string Key = "BuildJob";

    public IList<ClearMLBuildJobOptions> ClearML { get; set; } = new List<ClearMLBuildJobOptions>();
}
