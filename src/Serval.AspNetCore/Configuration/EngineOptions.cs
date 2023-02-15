namespace Serval.AspNetCore.Configuration;

public class EngineOptions
{
    public const string Key = "Engine";

    public List<Engine> Translation { get; set; } = new List<Engine>();
}

public class Engine
{
    public string Type { get; set; } = "";
    public string Address { get; set; } = "";
}
