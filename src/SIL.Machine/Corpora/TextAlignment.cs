using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class TextAlignment
	{
		public TextAlignment(object segRef, IEnumerable<AlignedWordPair> alignedWordPairs)
		{
			SegmentRef = segRef;
			AlignedWordPairs = alignedWordPairs.ToArray();
		}

		public object SegmentRef { get; }

		public IEnumerable<AlignedWordPair> AlignedWordPairs { get; }
	}
}
