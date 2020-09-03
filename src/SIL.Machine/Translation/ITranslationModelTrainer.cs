using System;

namespace SIL.Machine.Translation
{
	public interface ITranslationModelTrainer : IDisposable
	{
		SmtBatchTrainStats Stats { get; }

		void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null);
		void Save();
	}
}
