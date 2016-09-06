using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : ITranslationEngine
	{
		TranslationResult GetBestPhraseAlignment(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment);

		void Save();

		void Train(Func<string, string> sourcePreprocessor, ITokenizer<string, int> sourceTokenizer, ITextCorpus sourceCorpus,
			Func<string, string> targetPreprocessor, ITokenizer<string, int> targetTokenizer, ITextCorpus targetCorpus, IProgress progress = null);

		void Train(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, WordAlignmentMatrix matrix = null);
	}
}
