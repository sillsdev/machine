namespace SIL.Machine.AspNetCore.Models;

public class Corpus
{
    public string Id { get; set; } = default!;
    public string SourceLanguage { get; set; } = default!;
    public string TargetLanguage { get; set; } = default!;
    public bool Pretranslate { get; set; }
    public List<CorpusFile> SourceFiles { get; set; } = new List<CorpusFile>();
    public List<CorpusFile> TargetFiles { get; set; } = new List<CorpusFile>();
}
