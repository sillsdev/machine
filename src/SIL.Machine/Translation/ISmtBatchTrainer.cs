using System;

namespace SIL.Machine.Translation
{
	public interface ISmtBatchTrainer : IDisposable
	{
		void Train(IProgress<SmtTrainProgress> progress = null, Action checkCanceled = null);
		void Save();
	}
}
