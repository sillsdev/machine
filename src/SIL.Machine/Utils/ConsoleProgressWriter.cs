using System;

namespace SIL.Machine.Utils
{
	public class ConsoleProgressWriter : IProgress<ProgressStatus>
	{
		public string DefaultMessage { get; set; }

		public void Report(ProgressStatus status)
		{
			string message = status.Message;
			if (string.IsNullOrEmpty(message))
				message = DefaultMessage;
			string line = $"{status.PercentCompleted:P}";
			if (!string.IsNullOrEmpty(message))
				line = $"{message}: {line}";
			Console.WriteLine(line);
		}
	}
}