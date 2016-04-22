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
				int bestIndex = 0;
				double bestAlignmentScore = double.MinValue;
				for (int i = 0; i < sourceSegment.Count; i++)
				{
					if (IsPunctuation(targetSegment[j]) != IsPunctuation(sourceSegment[i]))
						continue;

					double confidence;
					if (IsNumber(targetSegment[j]) && targetSegment[j] == sourceSegment[i])
					{
						confidence = 1;
					}
					else
					{
						confidence = _segmentAligner.GetTranslationProbability(sourceSegment[i], targetSegment[j]);
					}

					double distance = (double) Math.Abs(i - j) / (Math.Max(targetSegment.Count, sourceSegment.Count) - 1);
					double alignmentScore = (Math.Log(targetSegment[j] == sourceSegment[i] ? 1.0f : confidence) * Alpha) + (Math.Log(1.0f - distance) * (1.0f - Alpha));

					if (alignmentScore > bestAlignmentScore)
					{
						bestIndex = i;
						bestAlignmentScore = alignmentScore;
					}
				}
				totalAlignmentScore += bestAlignmentScore;
				waMatrix[bestIndex, j] = true;
			}
			return totalAlignmentScore / sourceSegment.Count;
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			return _segmentAligner.GetTranslationProbability(sourceWord, targetWord);
		}

		private static bool IsPunctuation(string word)
		{
			return word.All(char.IsPunctuation);
		}

		private static bool IsNumber(string word)
		{
			return word.All(char.IsNumber);
		}
	}
}
