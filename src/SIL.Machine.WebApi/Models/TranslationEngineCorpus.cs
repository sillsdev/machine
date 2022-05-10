namespace SIL.Machine.WebApi.Models;

public class TranslationEngineCorpus
{
	public string CorpusRef { get; set; } = default!;
	public bool Pretranslate { get; set; }
}
