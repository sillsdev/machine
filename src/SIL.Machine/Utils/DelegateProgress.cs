using System;

namespace SIL.Machine.Utils
{
	public class DelegateProgress : IProgress<ProgressStatus>
	{
		private readonly Action<ProgressStatus> _report;

		public DelegateProgress(Action<ProgressStatus> report)
		{
			_report = report;
		}

		public void Report(ProgressStatus status)
		{
			_report(status);
		}
	}
}