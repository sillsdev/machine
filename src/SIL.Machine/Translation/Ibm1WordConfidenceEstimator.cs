using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
    public class Ibm1WordConfidenceEstimator : IWordConfidenceEstimator
    {
        private readonly Func<string, string, double> _getTranslationProb;
        private readonly IReadOnlyList<string> _sourceTokens;

        public Ibm1WordConfidenceEstimator(
            Func<string, string, double> getTranslationProb,
            IReadOnlyList<string> sourceTokens
        )
        {
            _getTranslationProb = getTranslationProb;
            _sourceTokens = sourceTokens;
        }

        public bool PhraseOnly { get; set; } = true;

        public double Estimate(Range<int> sourceSegmentRange, string targetWord)
        {
            if (!PhraseOnly)
                sourceSegmentRange = Range<int>.Create(0, _sourceTokens.Count);
            double maxConfidence = _getTranslationProb(null, targetWord);
            for (int i = sourceSegmentRange.Start; i < sourceSegmentRange.End; i++)
            {
                double confidence = _getTranslationProb(_sourceTokens[i], targetWord);
                if (confidence > maxConfidence)
                    maxConfidence = confidence;
            }

            return maxConfidence;
        }
    }
}
