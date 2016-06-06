namespace SIL.Machine.WebApi.Models
{
	public class SessionDto
	{
		public string Id { get; set; }
		public string SourceSegment { get; set; }
		public string Prefix { get; set; }
		public double ConfidenceThreshold { get; set; }
	}
}
