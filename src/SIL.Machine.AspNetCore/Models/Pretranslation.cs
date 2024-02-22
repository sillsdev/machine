namespace SIL.Machine.AspNetCore.Models;

public record Pretranslation
{
    public required string CorpusId { get; init; }
    public required string TextId { get; init; }
    public required IReadOnlyList<string> Refs { get; init; }
    public required string Translation { get; init; }
}
