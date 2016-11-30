using System.Collections.Generic;

namespace SIL.Machine.Translation.Thot
{
	public interface ISegmentAligner
	{
		double GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, out WordAlignmentMatrix waMatrix);
		double GetTranslationProbability(string sourceWord, string targetWord);
	}
}
