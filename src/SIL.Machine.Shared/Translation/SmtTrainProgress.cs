using System;

namespace SIL.Machine.Translation
{
	public struct SmtTrainProgress
	{
		public SmtTrainProgress(int currentStep, string currentStepMessage, int stepCount)
		{
			CurrentStep = currentStep;
			CurrentStepMessage = currentStepMessage;
			StepCount = stepCount;
		}

		public int CurrentStep { get; }
		public string CurrentStepMessage { get; }
		public int StepCount { get; }
		public int PercentCompleted => (int) Math.Round(((double) CurrentStep / StepCount) * 100.0, 0,
			MidpointRounding.AwayFromZero);
	}
}
