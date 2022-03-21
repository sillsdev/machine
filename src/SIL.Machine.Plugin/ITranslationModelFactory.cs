using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine.Plugin
{
	public interface ITranslationModelFactory
	{
		string ModelType { get; }

		ITranslationModel CreateModel(string modelPath);
		ITrainer CreateTrainer(string modelPath, IParallelTextCorpusView parallelCorpus, int maxCorpusCount);
	}
}
