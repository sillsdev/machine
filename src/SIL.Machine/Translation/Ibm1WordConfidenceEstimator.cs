using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class Ibm1WordConfidenceEstimator : IWordConfidenceEstimator
	{
		private readonly Func<string, string, double> _getTranslationProb;

		public Ibm1WordConfidenceEstimator(Func<string, string, double> getTranslationProb)
		{
			_getTranslationProb = getTranslationProb;
		}

		public void Estimate(IReadOnlyList<string> sourceSegment, WordGraph wordGraph)
		{
			foreach (WordGraphArc arc in wordGraph.Arcs)
			{
				for (int k = 0; k < arc.Words.Count; k++)
					arc.WordConfidences[k] = GetConfidence(sourceSegment, arc.Words[k]);
			}
		}

		public IReadOnlyList<double> Estimate(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			var confidences = new double[targetSegment.Count];
			for (int j = 0; j < targetSegment.Count; j++)
				confidences[j] = GetConfidence(sourceSegment, targetSegment[j]);
			return confidences;
		}

		private double GetConfidence(IReadOnlyList<string> sourceSegment, string targetWord)
		{
			double maxConfidence = _getTranslationProb(null, targetWord);
			foreach (string sourceWord in sourceSegment)
			{
				double confidence = _getTranslationProb(sourceWord, targetWord);
				if (confidence > maxConfidence)
					maxConfidence = confidence;
			}

			return maxConfidence;
		}
	}
}
