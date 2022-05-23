namespace SIL.Machine.WebApi.Configuration;

public class EngineOptions
{
    public string EnginesDir { get; set; } = "engines";
    public string DataFilesDir { get; set; } = "data";
    public TimeSpan EngineCommitFrequency { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan InactiveEngineTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan BuildLongPollTimeout { get; set; } = TimeSpan.FromSeconds(40);
}
