using System;
using System.Collections.Generic;

namespace SIL.Machine.WebApi.Options
{
	public class MachineOptions
	{
		public string Namespace { get; set; } = "machine-api";
		public IList<string> AuthenticationSchemes { get; } = new List<string>();
		public string EnginesDir { get; set; } = "engines";
		public TimeSpan EngineCommitFrequency { get; set; } = TimeSpan.FromMinutes(5);
		public TimeSpan InactiveEngineTimeout { get; set; } = TimeSpan.FromMinutes(10);
		public TimeSpan BuildLongPollTimeout { get; set; } = TimeSpan.FromSeconds(40);
	}
}
