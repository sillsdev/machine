namespace SIL.Machine.AspNetCore.Models;

public enum FileFormat
{
    Text = 0,
    Paratext = 1
}

public record CorpusFile
{
    public required string Location { get; init; }
    public required FileFormat Format { get; init; }
    public required string TextId { get; init; }
}
