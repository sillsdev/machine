namespace SIL.Machine.WebApi.Models;

public class TrainSegmentPair : IEntity<TrainSegmentPair>
{
	public TrainSegmentPair()
	{
	}

	public TrainSegmentPair(TrainSegmentPair textSegmentPair)
	{
		Id = textSegmentPair.Id;
		Revision = textSegmentPair.Revision;
		EngineRef = textSegmentPair.EngineRef;
		Source = textSegmentPair.Source.ToList();
		Target = textSegmentPair.Target.ToList();
		SentenceStart = textSegmentPair.SentenceStart;
	}

	public string Id { get; set; } = default!;
	public int Revision { get; set; } = 1;
	public string EngineRef { get; set; } = default!;
	public List<string> Source { get; set; } = new List<string>();
	public List<string> Target { get; set; } = new List<string>();
	public bool SentenceStart { get; set; }

	public TrainSegmentPair Clone()
	{
		return new TrainSegmentPair(this);
	}
}
