using System;

namespace SIL.Machine.Translation
{
	public interface ISmtBatchTrainer : IDisposable
	{
		SmtBatchTrainStats Stats { get; }

		void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null);
		void Save();
	}
}
