namespace SIL.Machine.AspNetCore.Configuration;

public class BuildJobOptions
{
    public const string Key = "BuildJob";

    public IList<ClearMLBuildQueue> ClearML { get; set; } = new List<ClearMLBuildQueue>();
}
