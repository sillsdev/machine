using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class TextAlignment
	{
		public TextAlignment(TextSegmentRef segRef, IEnumerable<(int SourceIndex, int TargetIndex)> alignedWords)
		{
			SegmentRef = segRef;
			AlignedWords = alignedWords.ToArray();
		}

		public TextSegmentRef SegmentRef { get; }

		public IEnumerable<(int SourceIndex, int TargetIndex)> AlignedWords { get; }
	}
}
