namespace SIL.Machine.WebApi.Controllers
{
	public class BuildDto
	{
		public string Id { get; set; }
		public long Revision { get; set; }
		public string Engine { get; set; }
		public int StepCount { get; set; }
		public int CurrentStep { get; set; }
		public string CurrentStepMessage { get; set; }
	}
}
