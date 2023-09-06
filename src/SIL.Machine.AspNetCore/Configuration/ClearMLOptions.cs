namespace SIL.Machine.AspNetCore.Configuration;

public class ClearMLOptions
{
    public const string Key = "ClearML";

    public string ApiServer { get; set; } = "http://localhost:8008";
    public string Queue { get; set; } = "default";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public bool BuildPollingEnabled { get; set; } = false;
    public TimeSpan BuildPollingTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public string ModelType { get; set; } = "huggingface";
    public int MaxSteps { get; set; } = 20_000;
    public string RootProject { get; set; } = "Machine";
    public string DockerImage { get; set; } = "";
}
