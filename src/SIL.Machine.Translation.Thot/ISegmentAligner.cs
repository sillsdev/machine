using System.Collections.Generic;

namespace SIL.Machine.Translation.Thot
{
	public interface ISegmentAligner
	{
		double GetBestAlignment(IList<string> sourceSegment, IList<string> targetSegment, out WordAlignmentMatrix waMatrix);
		double GetTranslationProbability(string sourceWord, string targetWord);
	}
}
