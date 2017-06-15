namespace SIL.Machine.WebApi.Models
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
			StepCount = build.StepCount;
			CurrentStep = build.CurrentStep;
			CurrentStepMessage = build.CurrentStepMessage;
		}

		public string Id { get; set; }
		public long Revision { get; set; }
		public string EngineId { get; set; }
		public int StepCount { get; set; }
		public int CurrentStep { get; set; }
		public string CurrentStepMessage { get; set; }

		public Build Clone()
		{
			return new Build(this);
		}
	}
}
