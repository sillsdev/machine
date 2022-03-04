namespace SIL.Machine.WebApi.Models;

public class Engine : IEntity<Engine>
{
	public Engine()
	{
	}

	public Engine(Engine engine)
	{
		Id = engine.Id;
		Revision = engine.Revision;
		SourceLanguageTag = engine.SourceLanguageTag;
		TargetLanguageTag = engine.TargetLanguageTag;
		Type = engine.Type;
		Owner = engine.Owner;
		IsBuilding = engine.IsBuilding;
		BuildRevision = engine.BuildRevision;
		Confidence = engine.Confidence;
		TrainedSegmentCount = engine.TrainedSegmentCount;
	}

	public string Id { get; set; } = default!;
	public int Revision { get; set; } = 1;
	public string SourceLanguageTag { get; set; } = default!;
	public string TargetLanguageTag { get; set; } = default!;
	public EngineType Type { get; set; }
	public string Owner { get; set; } = default!;
	public bool IsBuilding { get; set; }
	public int BuildRevision { get; set; }
	public double Confidence { get; set; }
	public int TrainedSegmentCount { get; set; }

	public Engine Clone()
	{
		return new Engine(this);
	}
}
