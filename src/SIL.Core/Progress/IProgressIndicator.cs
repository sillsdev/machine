using System.Threading;

namespace SIL.Progress
{
	public interface IProgressIndicator
	{
		double PercentCompleted { get; set; }
		void Finish();
		void Initialize();
		void IndicateUnknownProgress();
		SynchronizationContext SyncContext { get; set; }
	}
}