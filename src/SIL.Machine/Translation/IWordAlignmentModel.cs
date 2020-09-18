using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface IWordAlignmentModel : ISegmentAligner, IDisposable
	{
		IReadOnlyList<string> SourceWords { get; }
		IReadOnlyList<string> TargetWords { get; }

		ITrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITextCorpus sourceCorpus,
			ITokenProcessor targetPreprocessor, ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null);

		void Save();
		Task SaveAsync();

		double GetTranslationProbability(string sourceWord, string targetWord);
		double GetTranslationProbability(int sourceWordIndex, int targetWordIndex);
	}
}
