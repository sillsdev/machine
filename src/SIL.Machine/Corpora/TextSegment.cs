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
			IsEmpty = segment.Count == 0;
		}

		public TextSegment(object segRef, bool sentenceStart = true,
			bool inRange = false, bool rangeStart = false, bool isEmpty = true)
			: this(segRef, Array.Empty<string>(), sentenceStart, inRange, rangeStart)
		{
			IsEmpty = isEmpty;
		}

		public object SegmentRef { get; }

		public bool IsEmpty { get; }

		public bool SentenceStart { get; }

		public bool IsInRange { get; }
		public bool IsRangeStart { get; }


		public IReadOnlyList<string> Segment { get; }

		public override string ToString()
		{
			string segment;
			if (IsEmpty)
				segment = IsInRange ? "<range>" : "EMPTY";
			else if (Segment.Count > 0)
				segment = string.Join(" ", Segment);
			else
				segment = "NONEMPTY";
			return $"{SegmentRef} - {segment}";
		}
	}
}
