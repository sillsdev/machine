namespace SIL.Machine.AspNetCore.Models;

public record Corpus
{
    public required string Id { get; init; }
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public required bool TrainOnAll { get; init; }
    public required bool PretranslateAll { get; init; }
    public IReadOnlyDictionary<string, IReadOnlySet<int>>? TrainOnChapters { get; init; }
    public IReadOnlyDictionary<string, IReadOnlySet<int>>? PretranslateChapters { get; init; }
    public required IReadOnlySet<string> TrainOnTextIds { get; init; }
    public required IReadOnlySet<string> PretranslateTextIds { get; init; }
    public required IReadOnlyList<CorpusFile> SourceFiles { get; init; }
    public required IReadOnlyList<CorpusFile> TargetFiles { get; init; }
}
