using System;

namespace SIL.Machine.WebApi.Server.Options
{
	public class EngineOptions
	{
		public string RootDir { get; set; }
		public TimeSpan EngineCommitFrequency { get; set; }
		public TimeSpan InactiveEngineTimeout { get; set; }
	}
}
