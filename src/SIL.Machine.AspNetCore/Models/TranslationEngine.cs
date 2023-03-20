namespace SIL.Machine.AspNetCore.Models;

public enum BuildState
{
    None,
    Pending,
    Active
}

public class TranslationEngine : IEntity
{
    public string Id { get; set; } = default!;
    public int Revision { get; set; } = 1;
    public string EngineId { get; set; } = default!;
    public string SourceLanguage { get; set; } = default!;
    public string TargetLanguage { get; set; } = default!;
    public BuildState BuildState { get; set; } = BuildState.None;
    public bool IsCanceled { get; set; }
    public string? BuildId { get; set; }
    public int BuildRevision { get; set; }
    public string? JobId { get; set; }
}
