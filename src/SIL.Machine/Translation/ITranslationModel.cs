using System;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface ITranslationModel : IDisposable
	{
		ITranslationEngine CreateEngine();
		ITrainer CreateTrainer(ParallelTextCorpus corpus, ITokenProcessor sourcePreprocessor = null,
			ITokenProcessor targetPreprocessor = null, int maxCorpusCount = int.MaxValue);
	}
}
