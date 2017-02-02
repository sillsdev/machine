using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISegmentAligner
	{
		WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
			WordAlignmentMatrix hintMatrix = null);
		double GetTranslationProbability(string sourceWord, string targetWord);
	}
}
