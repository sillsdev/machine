using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine.Plugin
{
	public interface ITranslationModelFactory
	{
		string ModelType { get; }

		ITranslationModel CreateModel(string modelPath);
		ITranslationModelTrainer CreateTrainer(string modelPath, ITokenProcessor sourcePreprocessor,
			ITokenProcessor targetPreprocessor, ParallelTextCorpus parallelCorpus, int maxCorpusCount);
	}
}
