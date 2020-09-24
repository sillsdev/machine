using System;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class PhaseProgress : DisposableBase, IProgress<ProgressStatus>
	{
		private readonly PhasedProgressReporter _reporter;
		private double _percentCompleted;

		internal PhaseProgress(PhasedProgressReporter reporter, Phase phase)
		{
			_reporter = reporter;
			Phase = phase;
			_reporter.Report(new ProgressStatus(0));
		}

		public Phase Phase { get; }

		void IProgress<ProgressStatus>.Report(ProgressStatus value)
		{
			_percentCompleted = value.PercentCompleted;
			_reporter.Report(value);
		}

		protected override void DisposeManagedResources()
		{
			if (_percentCompleted < 1.0)
				_reporter.Report(new ProgressStatus(1.0));
		}
	}
}
