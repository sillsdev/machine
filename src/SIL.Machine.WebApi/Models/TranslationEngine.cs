namespace SIL.Machine.WebApi.Models;

public class TranslationEngine : IOwnedEntity
{
	public string Id { get; set; } = default!;
	public int Revision { get; set; } = 1;
	public string SourceLanguageTag { get; set; } = default!;
	public string TargetLanguageTag { get; set; } = default!;
	public TranslationEngineType Type { get; set; }
	public string Owner { get; set; } = default!;
	public List<TranslationEngineCorpus> Corpora { get; set; } = new List<TranslationEngineCorpus>();
	public bool IsBuilding { get; set; }
	public int ModelRevision { get; set; }
	public double Confidence { get; set; }
	public int TrainedSegmentCount { get; set; }
}
