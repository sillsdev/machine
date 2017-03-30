using System;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface ISmtModel : IDisposable
	{
		ISmtEngine CreateEngine();
		void Save();
		ISmtBatchTrainer CreateBatchTrainer(Func<string, string> sourcePreprocessor, ITextCorpus sourceCorpus, Func<string, string> targetPreprocessor,
			ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null);
	}
}
