using System;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
	public interface ITruecaserTrainer : IDisposable
	{
		void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null);
		Task SaveAsync();
	}
}
