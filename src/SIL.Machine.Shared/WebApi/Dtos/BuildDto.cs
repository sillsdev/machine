namespace SIL.Machine.WebApi.Dtos
{
	public class BuildDto : ResourceDto
	{
		public int Revision { get; set; }
		public ResourceDto Engine { get; set; }
		public int StepCount { get; set; }
		public int CurrentStep { get; set; }
		public string CurrentStepMessage { get; set; }
	}
}
