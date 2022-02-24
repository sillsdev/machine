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

	public string Id { get; set; } = default!;
	public int Revision { get; set; } = 0;
	public string EngineRef { get; set; } = default!;
	public double PercentCompleted { get; set; } = default;
	public string? Message { get; set; }
	public BuildState State { get; set; } = BuildState.Pending;
	public DateTime? DateFinished { get; set; }

	public Build Clone()
	{
		return new Build(this);
	}
}
