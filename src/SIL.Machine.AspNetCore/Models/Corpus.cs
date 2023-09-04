namespace SIL.Machine.AspNetCore.Models;

public class Corpus
{
    public string Id { get; set; } = default!;
    public string SourceLanguage { get; set; } = default!;
    public string TargetLanguage { get; set; } = default!;
    public bool PretranslateAll { get; set; }
    public HashSet<string> PretranslateTextIds { get; set; } = default!;
    public List<CorpusFile> SourceFiles { get; set; } = default!;
    public List<CorpusFile> TargetFiles { get; set; } = default!;
}
