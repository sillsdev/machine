using System;

namespace SIL.Machine.QualityEstimation
{
    public static class BookConfidence
    {
        public const double LowBookConfidenceThreshold = 0.42;

        public static bool IsBookConfidenceUnusuallyLow(double confidence, string bookId = null, string model = null)
        {
            if (!(confidence >= 0 && confidence <= 1))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(confidence),
                    confidence,
                    "The book confidence is invalid. It is calculated as the geometric mean of the segment "
                        + "confidences, and it must be between 0 and 1, inclusive."
                );
            }

            return confidence < LowBookConfidenceThreshold;
        }
    }
}
