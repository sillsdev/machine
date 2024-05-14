namespace SIL.Machine.AspNetCore.Configuration;

public class ClearMLEngineTypeOptions
{
    public const string Key = "ClearMLEngineType";

    public string EngineType { get; set; } = "";
    public string Queue { get; set; } = "default";
    public string ModelType { get; set; } = "";
    public string DockerImage { get; set; } = "";
}
