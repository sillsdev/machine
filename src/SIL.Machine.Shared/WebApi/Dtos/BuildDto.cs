namespace SIL.Machine.WebApi.Dtos
{
	public class BuildDto : ResourceDto
	{
		public int Revision { get; set; }
		public ResourceDto Engine { get; set; }
		public double PercentCompleted { get; set; }
		public string Message { get; set; }
	}
}
