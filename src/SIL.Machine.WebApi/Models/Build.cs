namespace SIL.Machine.WebApi.Models;

public class Build : IEntity
{
	public string Id { get; set; } = default!;
	public int Revision { get; set; } = 1;
	public string EngineRef { get; set; } = default!;
	public string? JobId { get; set; }
	public int Step { get; set; }
	public double? PercentCompleted { get; set; }
	public string? Message { get; set; }
	public BuildState State { get; set; } = BuildState.Pending;
	public DateTime? DateFinished { get; set; }
}
