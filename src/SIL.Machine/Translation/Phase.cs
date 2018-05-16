using System;

namespace SIL.Machine.Translation
{
	public class Phase : IProgress<ProgressStatus>
	{
		public string Message { get; set; }
		internal int Index { get; set; }
		internal PhasedProgress Progress { get; set; }

		void IProgress<ProgressStatus>.Report(ProgressStatus value)
		{
			double current = Index + value.PercentCompleted;
			string message = value.Message ?? Message;
			Progress.Report(new ProgressStatus(current / Progress.Count, message));
		}
	}
}
