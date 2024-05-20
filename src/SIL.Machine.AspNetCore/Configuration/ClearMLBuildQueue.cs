namespace SIL.Machine.AspNetCore.Configuration;

public class ClearMLBuildQueue
{
    public TranslationEngineType TranslationEngineType { get; set; }
    public string ModelType { get; set; } = "";
    public string Queue { get; set; } = "default";
    public string DockerImage { get; set; } = "";
}
