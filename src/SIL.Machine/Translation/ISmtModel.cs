using System;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface ISmtModel : IDisposable
	{
		ISmtEngine CreateEngine();
		void Save();
		void Train(Func<string, string> sourcePreprocessor, ITextCorpus sourceCorpus, Func<string, string> targetPreprocessor,
			ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null, IProgress<SmtTrainProgress> progress = null, Func<bool> canceled = null);
	}
}
