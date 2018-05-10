using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextSegment
	{
		public TextSegment(object segRef, IReadOnlyList<string> segment)
		{
			SegmentRef = segRef;
			Segment = segment;
		}

		public object SegmentRef { get; }

		public bool IsEmpty => Segment.Count == 0;

		public IReadOnlyList<string> Segment { get; }

		public override string ToString()
		{
			return string.Format("{0} - {1}", SegmentRef, string.Join(" ", Segment));
		}
	}
}
