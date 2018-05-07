using System;

namespace SIL.Machine.Translation.Thot
{
	public class ThotTrainProgressReporter
	{
		private readonly int _stepCount;
		private readonly IProgress<ProgressData> _progress;
		private readonly Action _checkCanceled;
		private int _currentStep = -1;
		private string _currentStepMessage;

		public ThotTrainProgressReporter(int stepCount, IProgress<ProgressData> progress, Action checkCanceled)
		{
			_stepCount = stepCount;
			_progress = progress;
			_checkCanceled = checkCanceled;
		}

		public void CheckCanceled()
		{
			_checkCanceled?.Invoke();
		}

		public void Step(string message = null, int step = -1)
		{
			CheckCanceled();

			if (_progress == null)
				return;

			if (step < 0)
				_currentStep++;
			else
				_currentStep = step;

			if (message != null)
				_currentStepMessage = message;

			_progress.Report(new ProgressData(_currentStep, _stepCount, _currentStepMessage));
		}
	}
}
