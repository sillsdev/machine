using System;

namespace SIL.Machine.Translation
{
	public class Phase : IProgress<ProgressData>
	{
		public string Message { get; set; }
		internal int Index { get; set; }
		internal PhasedProgress Progress { get; set; }

		void IProgress<ProgressData>.Report(ProgressData value)
		{
			int currentStep = (Index * 100)
				+ (int) Math.Round(value.PercentCompleted * 100, MidpointRounding.AwayFromZero);
			int stepCount = Progress.Count * 100;
			string currentStepMessage = value.CurrentStepMessage ?? Message;
			Progress.Report(new ProgressData(currentStep, stepCount, currentStepMessage));
		}
	}
}
