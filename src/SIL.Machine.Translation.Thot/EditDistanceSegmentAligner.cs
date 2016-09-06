using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Translation.Thot
{
	public class EditDistanceSegmentAligner : ISegmentAligner
	{
		private readonly ISegmentAligner _segmentAligner;
		private readonly SegmentScorer _scorer;

		public EditDistanceSegmentAligner(ISegmentAligner segmentAligner)
		{
			_segmentAligner = segmentAligner;
			_scorer = new SegmentScorer(_segmentAligner);
		}

		public double GetBestAlignment(IList<string> sourceSegment, IList<string> targetSegment, out WordAlignmentMatrix waMatrix)
		{
			var paa = new PairwiseAlignmentAlgorithm<IList<string>, int>(_scorer, sourceSegment, targetSegment, GetWordIndices)
			{
				Mode = AlignmentMode.Global,
				ExpansionCompressionEnabled = true,
				TranspositionEnabled = true
			};
			paa.Compute();
			Alignment<IList<string>, int> alignment = paa.GetAlignments().First();
			waMatrix = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
			for (int c = 0; c < alignment.ColumnCount; c++)
			{
				foreach (int i in alignment[0, c])
				{
					foreach (int j in alignment[1, c])
						waMatrix[i, j] = AlignmentType.Aligned;
				}
			}

			return Math.Log(alignment.NormalizedScore);
		}

		private static IEnumerable<int> GetWordIndices(IList<string> sequence, out int index, out int count)
		{
			index = 0;
			count = sequence.Count;
			return Enumerable.Range(index, count);
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			return _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
		}
	}
}
