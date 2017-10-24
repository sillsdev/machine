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

		public IWordConfidences Estimate(IReadOnlyList<string> sourceSegment, WordGraph wordGraph = null)
		{
			return new WordConfidences(_getTranslationProb, sourceSegment);
		}

		private class WordConfidences : IWordConfidences
		{
			private readonly Func<string, string, double> _getTranslationProb;
			private readonly IReadOnlyList<string> _sourceSegment;

			public WordConfidences(Func<string, string, double> getTranslationProb,
				IReadOnlyList<string> sourceSegment)
			{
				_getTranslationProb = getTranslationProb;
				_sourceSegment = sourceSegment;
			}

			public double GetConfidence(string targetWord)
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
