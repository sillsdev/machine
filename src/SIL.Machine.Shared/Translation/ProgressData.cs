namespace SIL.Machine.Translation
{
	public struct ProgressData
	{
		public ProgressData(int currentStep, int stepCount, string currentStepMessage = null)
		{
			CurrentStep = currentStep;
			StepCount = stepCount;
			CurrentStepMessage = currentStepMessage;
		}

		public int CurrentStep { get; }
		public int StepCount { get; }
		public string CurrentStepMessage { get; }
		public double PercentCompleted => (double) CurrentStep / StepCount;
	}
}
