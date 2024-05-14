namespace SIL.Machine.AspNetCore.Configuration;

public class ClearMLOptions
{
    public const string Key = "ClearML";

    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public bool BuildPollingEnabled { get; set; } = false;
    public TimeSpan BuildPollingTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public string RootProject { get; set; } = "Machine";
    public string Project { get; set; } = "dev";
}
