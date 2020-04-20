using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(ParallelText text, object segRef, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, IEnumerable<AlignedWordPair> alignedWordPairs = null,
			bool sourceInRange = false, bool targetInRange = false)
		{
			Text = text;
			SegmentRef = segRef;
			SourceSegment = sourceSegment;
			TargetSegment = targetSegment;
			AlignedWordPairs = alignedWordPairs?.ToArray();
			IsSourceInRange = sourceInRange;
			IsTargetInRange = targetInRange;
		}

		public ParallelText Text { get; }

		public object SegmentRef { get; }

		public bool IsEmpty => SourceSegment.Count == 0 || TargetSegment.Count == 0;

		public bool IsSourceInRange { get; }
		public bool IsTargetInRange { get; }

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public IEnumerable<AlignedWordPair> AlignedWordPairs { get; }
	}
}
