namespace SIL.Machine.WebApi.Models;

public class TrainSegmentPair : IEntity
{
	public string Id { get; set; } = default!;
	public int Revision { get; set; } = 1;
	public string TranslationEngineRef { get; set; } = default!;
	public List<string> Source { get; set; } = new List<string>();
	public List<string> Target { get; set; } = new List<string>();
	public bool SentenceStart { get; set; }
}
