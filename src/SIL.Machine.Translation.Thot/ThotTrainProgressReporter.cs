using System;

namespace SIL.Machine.Translation.Thot
{
	public class ThotTrainProgressReporter
	{
		private readonly int _stepCount;
		private readonly IProgress<SmtTrainProgress> _progress;
		private readonly Func<bool> _canceled;
		private int _currentStep = -1;
		private string _currentStepMessage;

		public ThotTrainProgressReporter(int stepCount, IProgress<SmtTrainProgress> progress, Func<bool> canceled)
		{
			_stepCount = stepCount;
			_progress = progress;
			_canceled = canceled;
		}

		public bool IsCanceled => _canceled != null && _canceled();

		public bool Step(string message = null, int step = -1)
		{
			if (IsCanceled)
				return true;

			if (_progress == null)
				return false;

			if (step < 0)
				_currentStep++;
			else
				_currentStep = step;

			if (message != null)
				_currentStepMessage = message;

			_progress.Report(new SmtTrainProgress(_currentStep, _currentStepMessage, _stepCount));
			return false;
		}
	}
}
