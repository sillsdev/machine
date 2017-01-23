using System.Collections.Generic;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(TextSegmentRef segRef, IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, WordAlignmentMatrix alignment = null)
		{
			SegmentRef = segRef;
			SourceSegment = sourceSegment;
			TargetSegment = targetSegment;
			Alignment = alignment;
		}

		public TextSegmentRef SegmentRef { get; }

		public bool IsEmpty => SourceSegment.Count == 0 || TargetSegment.Count == 0;

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public WordAlignmentMatrix Alignment { get; }
	}
}
