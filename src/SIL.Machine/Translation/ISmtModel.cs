using System;
using SIL.Machine.Corpora;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public interface ISmtModel : IDisposable
	{
		ISmtEngine CreateEngine();
		void Save();
		void Train(Func<string, string> sourcePreprocessor, ITextCorpus sourceCorpus, Func<string, string> targetPreprocessor,
			ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null, IProgress progress = null);
	}
}
