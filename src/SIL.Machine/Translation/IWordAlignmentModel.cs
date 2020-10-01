using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface IWordAlignmentModel : IWordAligner, IDisposable
	{
		IReadOnlyList<string> SourceWords { get; }
		IReadOnlyList<string> TargetWords { get; }

		ITrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor,
			ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue);

		double GetTranslationProbability(string sourceWord, string targetWord);
		double GetTranslationProbability(int sourceWordIndex, int targetWordIndex);
	}
}
