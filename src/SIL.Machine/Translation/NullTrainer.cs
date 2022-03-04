using System;
using System.Threading.Tasks;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class NullTrainer : DisposableBase, ITrainer
	{
		public TrainStats Stats { get; } = new TrainStats();

		public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
		}

		public void Save()
		{
		}

		public Task SaveAsync()
		{
			return Task.CompletedTask;
		}
	}
}
