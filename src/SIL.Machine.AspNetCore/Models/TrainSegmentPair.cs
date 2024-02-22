namespace SIL.Machine.AspNetCore.Models;

public record TrainSegmentPair : IEntity
{
    public string Id { get; set; } = "";
    public int Revision { get; set; } = 1;
    public required string TranslationEngineRef { get; init; }
    public required string Source { get; init; }
    public required string Target { get; init; }
    public required bool SentenceStart { get; init; }
}
