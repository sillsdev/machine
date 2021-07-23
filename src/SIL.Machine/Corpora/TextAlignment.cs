using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class TextAlignment
	{
		public TextAlignment(string textId, object segRef, IReadOnlyCollection<AlignedWordPair> alignedWordPairs)
		{
			TextId = textId;
			SegmentRef = segRef;
			AlignedWordPairs = alignedWordPairs;
		}

		public string TextId { get; }

		public object SegmentRef { get; }

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; }

		public TextAlignment Invert()
		{
			return new TextAlignment(TextId, SegmentRef,
				new HashSet<AlignedWordPair>(AlignedWordPairs.Select(wp => wp.Invert())));
		}
	}
}
