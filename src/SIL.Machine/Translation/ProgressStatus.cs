namespace SIL.Machine.Translation
{
	public struct ProgressStatus
	{
		public ProgressStatus(int currentStep, int stepCount, string message = null)
			: this((double) currentStep / stepCount, message)
		{
		}

		public ProgressStatus(double percentCompleted, string message = null)
		{
			PercentCompleted = percentCompleted;
			Message = message;
		}

		public double PercentCompleted { get; }
		public string Message { get; }
	}
}
