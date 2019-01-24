using System.Collections.Generic;

namespace SIL.Machine.WebApi.Configuration
{
	public class MachineConfig
	{
		public string Namespace { get; set; } = "machine-api";
		public IReadOnlyList<string> AuthenticationSchemes { get; set; } = new string[0];
	}
}
