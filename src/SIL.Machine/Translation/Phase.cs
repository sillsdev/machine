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
			var data = new ProgressData((value.PercentCompleted * 100) * (Index + 1), Progress.Count * 100,
				value.CurrentStepMessage ?? Message);
			Progress.Report(data);
		}
	}
}
