using System;

namespace SIL.Machine.WebApi.Models
{
	public class SessionOptions
	{
		public TimeSpan SessionIdleTimeout { get; set; }
		public TimeSpan StaleSessionCleanupFrequency { get; set; }
	}
}
