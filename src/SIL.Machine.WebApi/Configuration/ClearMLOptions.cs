namespace SIL.Machine.WebApi.Configuration;

public class ClearMLOptions
{
    public const string Key = "ClearML";

    public string ApiServer { get; set; } = "http://localhost:8008";
    public string Queue { get; set; } = "default";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Branch { get; set; } = "release";
    public TimeSpan BuildPollingTimeout { get; set; } = TimeSpan.FromSeconds(2);
}
