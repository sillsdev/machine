using System;
using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextSegment
	{
		public static TextSegment Create(string textId, object segRef, IReadOnlyList<string> segment,
			bool sentenceStart = true, bool inRange = false, bool rangeStart = false)
		{
			return new TextSegment(textId, segRef, segment, sentenceStart, inRange, rangeStart, segment.Count == 0);
		}

		public static TextSegment CreateNoText(string textId, object segRef, bool sentenceStart = true,
			bool inRange = false, bool rangeStart = false, bool isEmpty = true)
		{
			return new TextSegment(textId, segRef, Array.Empty<string>(), sentenceStart, inRange, rangeStart, isEmpty);
		}

		private TextSegment(string textId, object segRef, IReadOnlyList<string> segment, bool sentenceStart,
			bool inRange, bool rangeStart, bool isEmpty)
		{
			TextId = textId;
			SegmentRef = segRef;
			Segment = segment;
			SentenceStart = sentenceStart;
			IsInRange = inRange;
			IsRangeStart = rangeStart;
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
