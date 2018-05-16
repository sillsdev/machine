using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IWordAlignmentModel : ISegmentAligner, IDisposable
	{
		IReadOnlyList<string> SourceWords { get; }
		IReadOnlyList<string> TargetWords { get; }

		void AddSegmentPair(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
					WordAlignmentMatrix hintMatrix = null);
		void Train(IProgress<ProgressStatus> progress = null);
		void Save();

		double GetTranslationProbability(string sourceWord, string targetWord);
		double GetTranslationProbability(int sourceWordIndex, int targetWordIndex);
	}
}
