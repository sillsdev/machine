using System;
using SIL.ObjectModel;

namespace SIL.Machine.Utils
{
	public class PhaseProgress : DisposableBase, IProgress<ProgressStatus>
	{
		private readonly PhasedProgressReporter _reporter;
		private double? _percentCompleted;
		private int _step;

		internal PhaseProgress(PhasedProgressReporter reporter, Phase phase)
		{
			_reporter = reporter;
			Phase = phase;
			_reporter.Report(new ProgressStatus(_step, _percentCompleted));
		}

		public Phase Phase { get; }

		void IProgress<ProgressStatus>.Report(ProgressStatus value)
		{
			if (Phase.ReportSteps)
				_step = value.Step;
			_percentCompleted = value.PercentCompleted;
			_reporter.Report(value);
		}

		protected override void DisposeManagedResources()
		{
			if (_percentCompleted == null || _percentCompleted < 1.0)
				_reporter.Report(new ProgressStatus(_step + 1, 1.0));
		}
	}
}
