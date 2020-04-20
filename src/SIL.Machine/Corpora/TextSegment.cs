using System;
using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextSegment
	{
		public TextSegment(object segRef, IReadOnlyList<string> segment, bool inRange = false)
		{
			SegmentRef = segRef;
			Segment = segment;
			IsInRange = inRange;
		}

		public TextSegment(object segRef, bool inRange = false)
			: this(segRef, Array.Empty<string>(), inRange)
		{
		}

		public object SegmentRef { get; }

		public bool IsEmpty => Segment.Count == 0;

		public bool IsInRange { get; }

		public IReadOnlyList<string> Segment { get; }

		public override string ToString()
		{
			return string.Format("{0} - {1}", SegmentRef, string.Join(" ", Segment));
		}
	}
}
