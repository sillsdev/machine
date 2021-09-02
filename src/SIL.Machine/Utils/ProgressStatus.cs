namespace SIL.Machine.Utils
{
	public struct ProgressStatus
	{
		public ProgressStatus(int currentStep, int stepCount, string message = null)
			: this(stepCount == 0 ? 1.0 : (double)currentStep / stepCount, message)
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
