using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class SimpleSegmentAligner : ISegmentAligner
	{
		private const float Alpha = 0.5f;

		private readonly ISegmentAligner _segmentAligner;

		public SimpleSegmentAligner(ISegmentAligner segmentAligner)
		{
			_segmentAligner = segmentAligner;
		}

		public double GetBestAlignment(IList<string> sourceSegment, IList<string> targetSegment, out WordAlignmentMatrix waMatrix)
		{
			waMatrix = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
			double totalAlignmentScore = 0;
			for (int j = 0; j < targetSegment.Count; j++)
			{
				int bestIndex = -1;
				double bestAlignmentScore = ComputeAlignmentScore(_segmentAligner.GetTranslationProbability(null, targetSegment[j]), 0.5);
				for (int i = 0; i < sourceSegment.Count; i++)
				{
					if (IsPunctuation(targetSegment[j]) != IsPunctuation(sourceSegment[i]))
						continue;

					double probability = targetSegment[j] == sourceSegment[i] ? 1.0 : _segmentAligner.GetTranslationProbability(sourceSegment[i], targetSegment[j]);
					double distance = (double) Math.Abs(i - j) / (Math.Max(targetSegment.Count, sourceSegment.Count) - 1);
					double alignmentScore = ComputeAlignmentScore(probability, distance);

					if (alignmentScore > bestAlignmentScore)
					{
						bestIndex = i;
						bestAlignmentScore = alignmentScore;
					}
				}

				totalAlignmentScore += bestAlignmentScore;
				if (bestIndex > -1)
					waMatrix[bestIndex, j] = true;
			}
			return totalAlignmentScore;
		}

		private static double ComputeAlignmentScore(double probability, double distanceScore)
		{
			return (Math.Log(probability) * Alpha) + (Math.Log(1.0f - distanceScore) * (1.0f - Alpha));
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			return _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
		}

		private static bool IsPunctuation(string word)
		{
			return word.All(char.IsPunctuation);
		}
	}
}
