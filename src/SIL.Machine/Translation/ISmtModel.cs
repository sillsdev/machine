using System;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public interface ISmtModel : IDisposable
	{
		ISmtEngine CreateEngine();
		void Save();
		void Train(Func<string, string> sourcePreprocessor, ITokenizer<string, int> sourceTokenizer, ITextCorpus sourceCorpus,
			Func<string, string> targetPreprocessor, ITokenizer<string, int> targetTokenizer, ITextCorpus targetCorpus, IProgress progress = null);
	}
}
