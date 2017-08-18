namespace SIL.Machine.WebApi.Dtos
{
#if BRIDGE_NET
	[Bridge.ObjectLiteral(Bridge.ObjectInitializationMode.DefaultValue)]
#endif
	public class BuildDto : LinkDto
	{
		public string Id { get; set; }
		public int Revision { get; set; }
		public LinkDto Engine { get; set; }
		public int StepCount { get; set; }
		public int CurrentStep { get; set; }
		public string CurrentStepMessage { get; set; }
	}
}
