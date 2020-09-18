using System;
using System.Threading.Tasks;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface ITranslationModel : IDisposable
	{
		ITranslationEngine CreateEngine();
		void Save();
		Task SaveAsync();
		ITranslationModelTrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITextCorpus sourceCorpus,
			ITokenProcessor targetPreprocessor, ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null);
	}
}
