using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
	public class ParallelTextSegment
	{
		public ParallelTextSegment(ParallelText text, IComparable segRef, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, IEnumerable<(int SourceIndex, int TargetIndex)> alignedWords = null)
		{
			Text = text;
			SegmentRef = segRef;
			SourceSegment = sourceSegment;
			TargetSegment = targetSegment;
			AlignedWords = alignedWords?.ToArray();
		}

		public ParallelText Text { get; }

		public IComparable SegmentRef { get; }

		public bool IsEmpty => SourceSegment.Count == 0 || TargetSegment.Count == 0;

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public IEnumerable<(int SourceIndex, int TargetIndex)> AlignedWords { get; }

		public WordAlignmentMatrix CreateAlignmentMatrix(bool isUnknown)
		{
			if (AlignedWords == null)
				return null;

			var matrix = new WordAlignmentMatrix(SourceSegment.Count, TargetSegment.Count, isUnknown ? AlignmentType.Unknown : AlignmentType.NotAligned);
			foreach ((int SourceIndex, int TargetIndex) alignment in AlignedWords)
			{
				matrix[alignment.SourceIndex, alignment.TargetIndex] = AlignmentType.Aligned;
				if (isUnknown)
				{
					for (int i = 0; i < SourceSegment.Count; i++)
					{
						if (matrix[i, alignment.TargetIndex] == AlignmentType.Unknown)
							matrix[i, alignment.TargetIndex] = AlignmentType.NotAligned;
					}

					for (int j = 0; j < TargetSegment.Count; j++)
					{
						if (matrix[alignment.SourceIndex, j] == AlignmentType.Unknown)
							matrix[alignment.SourceIndex, j] = AlignmentType.NotAligned;
					}
				}
			}

			return matrix;
		}
	}
}
