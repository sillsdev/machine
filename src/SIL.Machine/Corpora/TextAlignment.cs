using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextAlignment
	{
		public TextAlignment(object segRef, IEnumerable<AlignedWordPair> alignedWordPairs)
		{
			SegmentRef = segRef;
			AlignedWordPairs = new HashSet<AlignedWordPair>(alignedWordPairs);
		}

		public object SegmentRef { get; }

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; }
	}
}
