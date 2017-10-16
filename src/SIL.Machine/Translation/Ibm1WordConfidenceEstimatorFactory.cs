using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class Ibm1WordConfidenceEstimatorFactory : IWordConfidenceEstimatorFactory
	{
		private readonly Func<string, string, double> _getTranslationProb;

		public Ibm1WordConfidenceEstimatorFactory(Func<string, string, double> getTranslationProb)
		{
			_getTranslationProb = getTranslationProb;
		}

		public IWordConfidenceEstimator Create(IReadOnlyList<string> sourceSegment)
		{
			return new WordConfidenceEstimator(_getTranslationProb, sourceSegment);
		}

		private class WordConfidenceEstimator : IWordConfidenceEstimator
		{
			private readonly Func<string, string, double> _getTranslationProb;
			private readonly IReadOnlyList<string> _sourceSegment;

			public WordConfidenceEstimator(Func<string, string, double> getTranslationProb,
				IReadOnlyList<string> sourceSegment)
			{
				_getTranslationProb = getTranslationProb;
				_sourceSegment = sourceSegment;
			}

			public double EstimateConfidence(string targetWord)
			{
				double maxConfidence = _getTranslationProb(null, targetWord);
				foreach (string sourceWord in _sourceSegment)
				{
					double confidence = _getTranslationProb(sourceWord, targetWord);
					if (confidence > maxConfidence)
						maxConfidence = confidence;
				}

				return maxConfidence;
			}
		}
	}
}
