using System;

namespace SIL.Machine.QualityEstimation
{
    public static class BookConfidence
    {
        public static readonly double LowBookConfidenceThreshold = 0.42;

        /// <summary>
        /// Determines whether a book confidence is unusually low, i.e. below
        /// <see cref="LowBookConfidenceThreshold"/>.
        /// </summary>
        /// <param name="confidence">
        /// The book confidence, calculated as the geometric mean of the segment confidences. Must be between 0 and 1,
        /// inclusive.
        /// </param>
        /// <param name="bookId">The book id. Reserved for future book-specific logic; not currently used.</param>
        /// <param name="model">The model name. Reserved for future model-specific logic; not currently used.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="confidence"/> is below <see cref="LowBookConfidenceThreshold"/>; otherwise,
        /// <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="confidence"/> is not between 0 and 1, inclusive.
        /// </exception>
        public static bool IsBookConfidenceUnusuallyLow(double confidence, string bookId = null, string model = null)
        {
            if (!(confidence >= 0 && confidence <= 1))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(confidence),
                    confidence,
                    "The book confidence must be between 0 and 1, inclusive."
                );
            }

            return confidence < LowBookConfidenceThreshold;
        }
    }
}
