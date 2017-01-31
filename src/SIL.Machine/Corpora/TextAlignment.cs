using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class TextAlignment
	{
		public TextAlignment(TextSegmentRef segRef, IEnumerable<Tuple<int, int>> alignedWords)
		{
			SegmentRef = segRef;
			AlignedWords = alignedWords.ToArray();
		}

		public TextSegmentRef SegmentRef { get; }

		public IEnumerable<Tuple<int, int>> AlignedWords { get; }
	}
}
