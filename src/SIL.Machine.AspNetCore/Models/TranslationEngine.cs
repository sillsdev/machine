namespace SIL.Machine.AspNetCore.Models;

public class TranslationEngine : IEntity
{
    public string Id { get; set; } = default!;
    public int Revision { get; set; } = 1;
    public string EngineId { get; set; } = default!;
    public string SourceLanguage { get; set; } = default!;
    public string TargetLanguage { get; set; } = default!;
    public bool IsModelPersisted { get; set; }
    public int BuildRevision { get; set; }
    public Build? CurrentBuild { get; set; }
}
