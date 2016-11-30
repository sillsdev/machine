using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Translation.Thot
{
	public class FuzzyEditDistanceSegmentAligner : ISegmentAligner
	{
		private const double DefaultAlpha = 0.2f;
		private const int DefaultMaxDistance = 3;

		private readonly ISegmentAligner _segmentAligner;
		private readonly SegmentScorer _scorer;
		private readonly int _maxDistance;
		private readonly double _alpha;

		public FuzzyEditDistanceSegmentAligner(ISegmentAligner segmentAligner, double alpha = DefaultAlpha, int maxDistance = DefaultMaxDistance)
		{
			_segmentAligner = segmentAligner;
			_alpha = alpha;
			_maxDistance = maxDistance;
			_scorer = new SegmentScorer(_segmentAligner);
		}

		public double GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, out WordAlignmentMatrix waMatrix)
		{
			var paa = new PairwiseAlignmentAlgorithm<IReadOnlyList<string>, int>(_scorer, sourceSegment, targetSegment, GetWordIndices)
			{
				Mode = AlignmentMode.Global,
				ExpansionCompressionEnabled = true,
				TranspositionEnabled = true
			};
			paa.Compute();
			Alignment<IReadOnlyList<string>, int> alignment = paa.GetAlignments().First();
			waMatrix = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
			double totalScore = 0;
			for (int c = 0; c < alignment.ColumnCount; c++)
			{
				foreach (int j in alignment[1, c])
				{
					double bestScore;
					int minIndex, maxIndex;
					if (alignment[0, c].IsNull)
					{
						double prob = _segmentAligner.GetTranslationProbability(null, targetSegment[j]);
						bestScore = ComputeAlignmentScore(prob, 0);
						int tc = c - 1;
						while (tc >= 0 && alignment[0, tc].IsNull)
							tc--;
						int i = tc == -1 ? 0 : alignment[0, tc].Last;
						minIndex = i;
						maxIndex = i + 1;
					}
					else
					{
						double prob = alignment[0, c].Average(i => _segmentAligner.GetTranslationProbability(sourceSegment[i], targetSegment[j]));
						bestScore = ComputeAlignmentScore(prob, 0);
						minIndex = alignment[0, c].First - 1;
						maxIndex = alignment[0, c].Last + 1;
					}

					int bestIndex = -1;
					for (int i = minIndex; i >= Math.Max(0, minIndex - _maxDistance); i--)
					{
						double prob = _segmentAligner.GetTranslationProbability(sourceSegment[i], targetSegment[j]);
						double distanceScore = ComputeDistanceScore(i, minIndex + 1, sourceSegment.Count);
						double score = ComputeAlignmentScore(prob, distanceScore);
						if (score > bestScore)
						{
							bestScore = score;
							bestIndex = i;
						}
					}

					for (int i = maxIndex; i < Math.Min(sourceSegment.Count, maxIndex + _maxDistance); i++)
					{
						double prob = _segmentAligner.GetTranslationProbability(sourceSegment[i], targetSegment[j]);
						double distanceScore = ComputeDistanceScore(i, maxIndex - 1, sourceSegment.Count);
						double score = ComputeAlignmentScore(prob, distanceScore);
						if (score > bestScore)
						{
							bestScore = score;
							bestIndex = i;
						}
					}

					if (bestIndex == -1)
					{
						if (!alignment[0, c].IsNull)
						{
							waMatrix[minIndex + 1, j] = AlignmentType.Aligned;
							waMatrix[maxIndex - 1, j] = AlignmentType.Aligned;
						}
					}
					else
					{
						waMatrix[bestIndex, j] = AlignmentType.Aligned;
					}
					totalScore += bestScore;
				}
			}

			return totalScore;
		}

		private static IEnumerable<int> GetWordIndices(IReadOnlyList<string> sequence, out int index, out int count)
		{
			index = 0;
			count = sequence.Count;
			return Enumerable.Range(index, count);
		}

		private static double ComputeDistanceScore(int i1, int i2, int sourceLength)
		{
			return (double) Math.Abs(i1 - i2) / (sourceLength - 1);
		}

		private double ComputeAlignmentScore(double probability, double distanceScore)
		{
			return (Math.Log(probability) * _alpha) + (Math.Log(1.0f - distanceScore) * (1.0f - _alpha));
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			return _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
		}
	}
}
