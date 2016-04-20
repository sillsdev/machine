using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class SimpleSingleWordAlignmentModel : ISingleWordAlignmentModel
	{
		private const float Alpha = 0.5f;

		private readonly ISingleWordAlignmentModel _swAlignModel;

		public SimpleSingleWordAlignmentModel(ISingleWordAlignmentModel swAlignModel)
		{
			_swAlignModel = swAlignModel;
		}

		public int[] GetBestAlignment(IList<string> sourceSegment, IList<string> targetSegment)
		{
			var alignment = new int[targetSegment.Count];
			for (int i = 0; i < targetSegment.Count; i++)
			{
				int bestIndex = 0;
				double bestAlignmentScore = double.MinValue;
				for (int j = 0; j < sourceSegment.Count; j++)
				{
					if (IsPunctuation(targetSegment[i]) != IsPunctuation(sourceSegment[j]))
						continue;

					double confidence;
					if (IsNumber(targetSegment[i]) && targetSegment[i] == sourceSegment[j])
					{
						confidence = 1;
					}
					else
					{
						confidence = _swAlignModel.GetTranslationProbability(sourceSegment[j], targetSegment[i]);
					}

					double distance = (double) Math.Abs(i - j) / (Math.Max(targetSegment.Count, sourceSegment.Count) - 1);
					double alignmentScore = (Math.Log(targetSegment[i] == sourceSegment[j] ? 1.0f : confidence) * Alpha) + (Math.Log(1.0f - distance) * (1.0f - Alpha));

					if (alignmentScore > bestAlignmentScore)
					{
						bestIndex = j;
						bestAlignmentScore = alignmentScore;
					}
				}

				alignment[i] = bestIndex;
			}
			return alignment;
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			return _swAlignModel.GetTranslationProbability(sourceWord, targetWord);
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
