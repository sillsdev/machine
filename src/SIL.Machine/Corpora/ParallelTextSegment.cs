using System;
using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(string textId, object sourceSegmentRef, object targetSegmentRef,
			IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
			IReadOnlyCollection<AlignedWordPair> alignedWordPairs, bool isSourceSentenceStart, bool isSourceInRange,
			bool isSourceRangeStart, bool isTargetSentenceStart, bool isTargetInRange, bool isTargetRangeStart,
			bool isEmpty)
		{
			if (sourceSegmentRef == null && targetSegmentRef == null)
				throw new ArgumentNullException("Either a source or target segment ref must be provided.");

			TextId = textId;
			SourceSegmentRef = sourceSegmentRef;
			TargetSegmentRef = targetSegmentRef;
			SourceSegment = sourceSegment;
			TargetSegment = targetSegment;
			AlignedWordPairs = alignedWordPairs;
			IsSourceSentenceStart = isSourceSentenceStart;
			IsSourceInRange = isSourceInRange;
			IsSourceRangeStart = isSourceRangeStart;
			IsTargetSentenceStart = isTargetSentenceStart;
			IsTargetInRange = isTargetInRange;
			IsTargetRangeStart = isTargetRangeStart;
			IsEmpty = isEmpty;
		}

		public string TextId { get; }

		public object SegmentRef => SourceSegmentRef ?? TargetSegmentRef;

		public object SourceSegmentRef { get; }
		public object TargetSegmentRef { get; }

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; }

		public bool IsSourceSentenceStart { get; }
		public bool IsSourceInRange { get; }
		public bool IsSourceRangeStart { get; }
		public bool IsTargetSentenceStart { get; }
		public bool IsTargetInRange { get; }
		public bool IsTargetRangeStart { get; }

		public bool IsEmpty { get; }
	}
}
