namespace SIL.Machine.WebApi.Models;

public class Engine : IEntity<Engine>
{
	public Engine()
	{
	}

	public Engine(Engine engine)
	{
		Id = engine.Id;
		SourceLanguageTag = engine.SourceLanguageTag;
		TargetLanguageTag = engine.TargetLanguageTag;
		Confidence = engine.Confidence;
		TrainedSegmentCount = engine.TrainedSegmentCount;
	}

	public string Id { get; set; } = default!;
	public string SourceLanguageTag { get; set; } = default!;
	public string TargetLanguageTag { get; set; } = default!;
	public string Type { get; set; } = default!;
	public double Confidence { get; set; } = default;
	public int TrainedSegmentCount { get; set; } = default;

	public Engine Clone()
	{
		return new Engine(this);
	}
}
