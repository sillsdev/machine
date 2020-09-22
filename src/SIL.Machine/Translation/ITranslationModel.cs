using System;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface ITranslationModel : IDisposable
	{
		ITranslationEngine CreateEngine();
		ITranslationModelTrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor,
			ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue);
	}
}
