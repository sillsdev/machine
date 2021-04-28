using System;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
	public interface ITrainer : IDisposable
	{
		void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null);
		Task SaveAsync();
		void Save();

		TrainStats Stats { get; }
	}
}
