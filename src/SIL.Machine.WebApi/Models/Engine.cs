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
		Confidence = engine.Confidence;
		TrainedSegmentCount = engine.TrainedSegmentCount;
	}

	public string Id { get; set; }
	public int Revision { get; set; }
	public string SourceLanguageTag { get; set; }
	public string TargetLanguageTag { get; set; }
	public double Confidence { get; set; }
	public int TrainedSegmentCount { get; set; }

	public Engine Clone()
	{
		return new Engine(this);
	}
}
