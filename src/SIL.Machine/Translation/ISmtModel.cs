using System;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface ISmtModel : IDisposable
	{
		ISmtEngine CreateEngine();
		void Save();
		ISmtBatchTrainer CreateBatchTrainer(ITokenProcessor sourcePreprocessor, ITextCorpus sourceCorpus,
			ITokenProcessor targetPreprocessor, ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null);
	}
}
