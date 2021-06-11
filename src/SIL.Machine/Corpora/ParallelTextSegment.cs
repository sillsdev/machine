using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(ParallelText text, TextSegment sourceSegment, TextSegment targetSegment,
			IEnumerable<AlignedWordPair> alignedWordPairs = null)
		{
			Text = text;
			SegmentRef = sourceSegment != null ? sourceSegment.SegmentRef : targetSegment.SegmentRef;
			SourceSegment = sourceSegment != null ? sourceSegment.Segment : Array.Empty<string>();
			TargetSegment = targetSegment != null ? targetSegment.Segment : Array.Empty<string>();
			AlignedWordPairs = alignedWordPairs?.ToArray();
			IsSourceInRange = sourceSegment != null && sourceSegment.IsInRange;
			IsSourceRangeStart = sourceSegment != null && sourceSegment.IsRangeStart;
			IsTargetInRange = targetSegment != null && targetSegment.IsInRange;
			IsTargetRangeStart = targetSegment != null && targetSegment.IsRangeStart;
			IsEmpty = sourceSegment == null || sourceSegment.IsEmpty || targetSegment == null || targetSegment.IsEmpty;
		}

		public ParallelTextSegment(ParallelText text, object segRef, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, bool isEmpty)
		{
			Text = text;
			SegmentRef = segRef;
			SourceSegment = sourceSegment;
			TargetSegment = targetSegment;
			IsEmpty = isEmpty;
		}

		public ParallelTextSegment(ParallelText text, object segRef, bool isEmpty = true)
		{
			Text = text;
			SegmentRef = segRef;
			SourceSegment = Array.Empty<string>();
			TargetSegment = Array.Empty<string>();
			IsEmpty = isEmpty;
		}

		public ParallelText Text { get; }

		public object SegmentRef { get; }

		public bool IsEmpty { get; }

		public bool IsSourceInRange { get; }
		public bool IsSourceRangeStart { get; }
		public bool IsTargetInRange { get; }
		public bool IsTargetRangeStart { get; }

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; }
	}
}
