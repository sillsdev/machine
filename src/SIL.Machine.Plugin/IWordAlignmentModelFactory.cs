using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine.Plugin
{
	public interface IWordAlignmentModelFactory
	{
		string ModelType { get; }

		IWordAlignmentModel CreateModel(string modelPath);
		ITrainer CreateTrainer(string modelPath, ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor,
			ParallelTextCorpus parallelCorpus, int maxCorpusCount);
	}
}
