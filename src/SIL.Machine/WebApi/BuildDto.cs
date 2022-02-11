namespace SIL.Machine.WebApi
{
	public class BuildDto : ResourceDto
	{
		public int Revision { get; set; }
		public ResourceDto Engine { get; set; }
		public double PercentCompleted { get; set; }
		public string Message { get; set; }
		/// <summary>
		/// The current build job state.
		/// </summary>
		public string State { get; set; }
	}
}
