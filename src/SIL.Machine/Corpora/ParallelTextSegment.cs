using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(ParallelText text, IComparable segRef, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, IEnumerable<AlignedWordPair> alignedWordPairs = null)
		{
			Text = text;
			SegmentRef = segRef;
			SourceSegment = sourceSegment;
			TargetSegment = targetSegment;
			AlignedWordPairs = alignedWordPairs?.ToArray();
		}

		public ParallelText Text { get; }

		public IComparable SegmentRef { get; }

		public bool IsEmpty => SourceSegment.Count == 0 || TargetSegment.Count == 0;

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public IEnumerable<AlignedWordPair> AlignedWordPairs { get; }
	}
}
