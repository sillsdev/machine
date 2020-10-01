using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IWordAligner
	{
		WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment);
	}
}
