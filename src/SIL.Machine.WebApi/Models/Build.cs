namespace SIL.Machine.WebApi.Models;

public class Build : IEntity<Build>
{
	public Build()
	{
	}

	public Build(Build build)
	{
		Id = build.Id;
		Revision = build.Revision;
		EngineRef = build.EngineRef;
		PercentCompleted = build.PercentCompleted;
		Message = build.Message;
		State = build.State;
		DateFinished = build.DateFinished;
	}

	public string Id { get; set; }
	public int Revision { get; set; }
	public string EngineRef { get; set; }
	public double PercentCompleted { get; set; }
	public string Message { get; set; }
	public string State { get; set; } = BuildStates.Pending;
	public DateTime DateFinished { get; set; }

	public Build Clone()
	{
		return new Build(this);
	}
}
