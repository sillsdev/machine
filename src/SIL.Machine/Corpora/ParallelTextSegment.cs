using System;
using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public static ParallelTextSegment Create(string textId, TextSegment sourceSegment, TextSegment targetSegment,
			IReadOnlyCollection<AlignedWordPair> alignedWordPairs = null)
		{
			return new ParallelTextSegment(textId,
				sourceSegment != null ? sourceSegment.SegmentRef : targetSegment.SegmentRef,
				sourceSegment != null ? sourceSegment.Segment : Array.Empty<string>(),
				targetSegment != null ? targetSegment.Segment : Array.Empty<string>(),
				alignedWordPairs, sourceSegment != null && sourceSegment.IsInRange,
				sourceSegment != null && sourceSegment.IsRangeStart, targetSegment != null && targetSegment.IsInRange,
				targetSegment != null && targetSegment.IsRangeStart,
				sourceSegment == null || sourceSegment.IsEmpty || targetSegment == null || targetSegment.IsEmpty);
		}

		public static ParallelTextSegment CreateRange(string textId, object segRef, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, bool isEmpty)
		{
			return new ParallelTextSegment(textId, segRef, sourceSegment, targetSegment, null, false, false, false,
				false, isEmpty);
		}

		private ParallelTextSegment(string textId, object segRef, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, IReadOnlyCollection<AlignedWordPair> alignedWordPairs,
			bool isSourceInRange, bool isSourceRangeStart, bool isTargetInRange, bool isTargetRangeStart, bool isEmpty)
		{
			TextId = textId;
			SegmentRef = segRef;
			SourceSegment = sourceSegment;
			TargetSegment = targetSegment;
			AlignedWordPairs = alignedWordPairs;
			IsSourceInRange = isSourceInRange;
			IsSourceRangeStart = isSourceRangeStart;
			IsTargetInRange = isTargetInRange;
			IsTargetRangeStart = isTargetRangeStart;
			IsEmpty = isEmpty;
		}

		public string TextId { get; }

		public object SegmentRef { get; }

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; }

		public bool IsSourceInRange { get; }
		public bool IsSourceRangeStart { get; }
		public bool IsTargetInRange { get; }
		public bool IsTargetRangeStart { get; }

		public bool IsEmpty { get; }
	}
}
