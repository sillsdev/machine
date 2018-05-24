using System;

namespace SIL.Machine.WebApi.Server.Models
{
	public class Build : IEntity<Build>
	{
		public Build()
		{
		}

		public Build(Build build)
		{
			Id = build.Id;
			Revision = build.Revision;
			EngineId = build.EngineId;
			PercentCompleted = build.PercentCompleted;
			Message = build.Message;
			State = build.State;
			DateFinished = build.DateFinished;
		}

		public string Id { get; set; }
		public int Revision { get; set; }
		public string EngineId { get; set; }
		public double PercentCompleted { get; set; }
		public string Message { get; set; }
		public string State { get; set; } = BuildStates.Active;
		public DateTime DateFinished { get; set; }

		public Build Clone()
		{
			return new Build(this);
		}
	}
}
