namespace Serval.AspNetCore.Models;

public class TranslationEngineCorpus
{
    public string CorpusRef { get; set; } = default!;
    public bool Pretranslate { get; set; }
}
