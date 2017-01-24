using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISegmentAligner
	{
		WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment);
		double GetTranslationProbability(string sourceWord, string targetWord);
	}
}
