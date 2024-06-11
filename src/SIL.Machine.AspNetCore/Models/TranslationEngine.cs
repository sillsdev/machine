namespace SIL.Machine.AspNetCore.Models;

public record TranslationEngine : IEntity
{
    public string Id { get; set; } = "";
    public int Revision { get; set; } = 1;
    public required string EngineId { get; init; }
    public required TranslationEngineType Type { get; init; }
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public required bool IsModelPersisted { get; init; }
    public int BuildRevision { get; init; }
    public Build? CurrentBuild { get; init; }
}
