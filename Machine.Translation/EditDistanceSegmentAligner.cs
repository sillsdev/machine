using System.Collections.Generic;
using System.Linq;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Translation
{
	public class EditDistanceSegmentAligner : ISegmentAligner
	{
		private readonly ISegmentAligner _segmentAligner;
		private readonly Scorer _scorer;

		public EditDistanceSegmentAligner(ISegmentAligner segmentAligner)
		{
			_segmentAligner = segmentAligner;
			_scorer = new Scorer(_segmentAligner);
		}

		public double GetBestAlignment(IList<string> sourceSegment, IList<string> targetSegment, out WordAlignmentMatrix waMatrix)
		{
			var paa = new PairwiseAlignmentAlgorithm<IList<string>, int>(_scorer, sourceSegment, targetSegment, GetWordIndices)
			{
				Mode = AlignmentMode.Global, ExpansionCompressionEnabled = true
			};
			paa.Compute();
			Alignment<IList<string>, int> alignment = paa.GetAlignments().First();
			waMatrix = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
			for (int c = 0; c < alignment.ColumnCount; c++)
			{
				foreach (int i in alignment[0, c])
				{
					foreach (int j in alignment[1, c])
						waMatrix[i, j] = true;
				}
			}

			return alignment.RawScore;
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

		private class Scorer : IPairwiseAlignmentScorer<IList<string>, int>
		{
			private readonly ISegmentAligner _segmentAligner;

			public Scorer(ISegmentAligner segmentAligner)
			{
				_segmentAligner = segmentAligner;
			}

			public int GetGapPenalty(IList<string> sequence1, IList<string> sequence2)
			{
				return -100;
			}

			public int GetInsertionScore(IList<string> sequence1, int p, IList<string> sequence2, int q)
			{
				string targetWord = sequence2[q];
				return (int) (_segmentAligner.GetTranslationProbability(null, targetWord) * 1000.0);
			}

			public int GetDeletionScore(IList<string> sequence1, int p, IList<string> sequence2, int q)
			{
				string sourceWord = sequence1[p];
				return (int) (_segmentAligner.GetTranslationProbability(sourceWord, null) * 1000.0);
			}

			public int GetSubstitutionScore(IList<string> sequence1, int p, IList<string> sequence2, int q)
			{
				string sourceWord = sequence1[p];
				string targetWord = sequence2[q];
				return (int) ((sourceWord == targetWord ? 1.0 : _segmentAligner.GetTranslationProbability(sourceWord, targetWord)) * 1000.0);
			}

			public int GetExpansionScore(IList<string> sequence1, int p, IList<string> sequence2, int q1, int q2)
			{
				return ((GetSubstitutionScore(sequence1, p, sequence2, q1) + GetSubstitutionScore(sequence1, p, sequence2, q2)) / 2) + 100;
			}

			public int GetCompressionScore(IList<string> sequence1, int p1, int p2, IList<string> sequence2, int q)
			{
				return ((GetSubstitutionScore(sequence1, p1, sequence2, q) + GetSubstitutionScore(sequence1, p2, sequence2, q)) / 2) + 100;
			}

			public int GetMaxScore1(IList<string> sequence1, int p, IList<string> sequence2)
			{
				return 1000;
			}

			public int GetMaxScore2(IList<string> sequence1, IList<string> sequence2, int q)
			{
				return 1000;
			}
		}
	}
}
