using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(TextSegmentRef segRef, IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, IEnumerable<Tuple<int, int>> alignedWords = null)
		{
			SegmentRef = segRef;
			SourceSegment = sourceSegment;
			TargetSegment = targetSegment;
			AlignedWords = alignedWords?.ToArray();
		}

		public TextSegmentRef SegmentRef { get; }

		public bool IsEmpty => SourceSegment.Count == 0 || TargetSegment.Count == 0;

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public IEnumerable<Tuple<int, int>> AlignedWords { get; }

		public WordAlignmentMatrix GetAlignmentMatrix(bool isUnknown)
		{
			if (AlignedWords == null)
				return null;

			var matrix = new WordAlignmentMatrix(SourceSegment.Count, TargetSegment.Count, isUnknown ? AlignmentType.Unknown : AlignmentType.NotAligned);
			foreach (Tuple<int, int> alignment in AlignedWords)
			{
				matrix[alignment.Item1, alignment.Item2] = AlignmentType.Aligned;
				if (isUnknown)
				{
					for (int i = 0; i < SourceSegment.Count; i++)
					{
						if (matrix[i, alignment.Item2] == AlignmentType.Unknown)
							matrix[i, alignment.Item2] = AlignmentType.NotAligned;
					}

					for (int j = 0; j < TargetSegment.Count; j++)
					{
						if (matrix[alignment.Item1, j] == AlignmentType.Unknown)
							matrix[alignment.Item1, j] = AlignmentType.NotAligned;
					}
				}
			}

			return matrix;
		}
	}
}
