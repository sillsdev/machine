using System.Threading;

namespace SIL.Progress
{
	public class NullProgressIndicator : IProgressIndicator
	{
		public double PercentCompleted { get; set; }

		public void Finish()
		{
		}

		public void Initialize()
		{
		}

		public void IndicateUnknownProgress()
		{
		}

		public SynchronizationContext SyncContext { get; set; }
	}
}