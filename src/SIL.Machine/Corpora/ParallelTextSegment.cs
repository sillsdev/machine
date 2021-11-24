using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(string textId, object segRef, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, IReadOnlyCollection<AlignedWordPair> alignedWordPairs,
			bool isSourceSentenceStart, bool isSourceInRange, bool isSourceRangeStart, bool isTargetSentenceStart,
			bool isTargetInRange, bool isTargetRangeStart, bool isEmpty)
		{
			TextId = textId;
			SegmentRef = segRef;
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

		public object SegmentRef { get; }

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
