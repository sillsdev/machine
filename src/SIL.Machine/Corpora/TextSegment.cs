using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextSegment
	{
		public TextSegment(string textId, object segRef, IReadOnlyList<string> segment, bool isSentenceStart,
			bool isInRange, bool isRangeStart, bool isEmpty)
		{
			TextId = textId;
			SegmentRef = segRef;
			Segment = segment;
			SentenceStart = isSentenceStart;
			IsInRange = isInRange;
			IsRangeStart = isRangeStart;
			IsEmpty = isEmpty;
		}

		public string TextId { get; }

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
