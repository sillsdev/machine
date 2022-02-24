using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
	public class BuildDto : ResourceDto
	{
		public int Revision { get; set; }
		[JsonProperty(Required = Required.DisallowNull)]
		public ResourceDto Engine { get; set; }
		public double PercentCompleted { get; set; }
		public string Message { get; set; }
		/// <summary>
		/// The current build job state.
		/// </summary>
		[JsonProperty(Required = Required.DisallowNull)]
		public BuildState State { get; set; }
	}
}
