using System.Collections.Generic;
using System.Linq;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Translation
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

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			var paa = new PairwiseAlignmentAlgorithm<IReadOnlyList<string>, int>(_scorer, sourceSegment, targetSegment, GetWordIndices)
			{
				Mode = AlignmentMode.Global,
				ExpansionCompressionEnabled = true,
				TranspositionEnabled = true
			};
			paa.Compute();
			Alignment<IReadOnlyList<string>, int> alignment = paa.GetAlignments().First();
			var waMatrix = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
			for (int c = 0; c < alignment.ColumnCount; c++)
			{
				foreach (int i in alignment[0, c])
				{
					foreach (int j in alignment[1, c])
						waMatrix[i, j] = AlignmentType.Aligned;
				}
			}

			return waMatrix;
		}

		private static IEnumerable<int> GetWordIndices(IReadOnlyList<string> sequence, out int index, out int count)
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
