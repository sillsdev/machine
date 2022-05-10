namespace SIL.Machine.WebApi.Models;

public class Pretranslation : IEntity
{
	public string Id { get; set; } = default!;
	public int Revision { get; set; } = 1;
	public string TranslationEngineRef { get; set; } = default!;
	public string CorpusRef { get; set; } = default!;
	public string TextId { get; set; } = default!;
	public List<string> Refs { get; set; } = new List<string>();
	public string Text { get; set; } = default!;
}
