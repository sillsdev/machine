namespace SIL.Machine.AspNetCore.Configuration;

public class ClearMLBuildJobOptions
{
    public const string Key = "ClearMLBuildJob";

    public string Queue { get; set; } = "default";
    public string DockerImage { get; set; } = "";
}
