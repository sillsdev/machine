using System;
using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextSegment
	{
		public TextSegment(object segRef, IReadOnlyList<string> segment, bool sentenceStart = true,
			bool inRange = false, bool rangeStart = false)
		{
			SegmentRef = segRef;
			Segment = segment;
			SentenceStart = sentenceStart;
			IsInRange = inRange;
			IsRangeStart = rangeStart;
		}

		public TextSegment(object segRef, bool inRange = false)
			: this(segRef, Array.Empty<string>(), inRange: inRange)
		{
		}

		public object SegmentRef { get; }

		public bool IsEmpty => Segment.Count == 0;

		public bool SentenceStart { get; }

		public bool IsInRange { get; }
		public bool IsRangeStart { get; }


		public IReadOnlyList<string> Segment { get; }

		public override string ToString()
		{
			return string.Format("{0} - {1}", SegmentRef, string.Join(" ", Segment));
		}
	}
}
