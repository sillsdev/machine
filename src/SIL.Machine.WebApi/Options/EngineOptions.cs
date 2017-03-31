using System;

namespace SIL.Machine.WebApi.Options
{
	public class EngineOptions
	{
		public string RootDir { get; set; }
		public string TrainProgressDir { get; set; }
		public TimeSpan EngineCommitFrequency { get; set; }
		public TimeSpan InactiveEngineTimeout { get; set; }
	}
}
