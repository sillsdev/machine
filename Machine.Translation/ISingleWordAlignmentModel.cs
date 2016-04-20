using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISingleWordAlignmentModel
	{
		int[] GetBestAlignment(IList<string> sourceSegment, IList<string> targetSegment);
		double GetTranslationProbability(string sourceWord, string targetWord);
	}
}
