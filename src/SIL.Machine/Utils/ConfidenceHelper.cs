using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Utils
{
    public static class ConfidenceHelper
    {
        /// <summary>
        /// Calculates the geometric mean for a collection of values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns>The geometric mean.</returns>
        public static double GeometricMean(IList<double> values)
        {
            // Geometric mean requires positive values
            if (values == null || !values.Any() || values.Any(x => x <= 0))
                return 0;

            // Compute the sum of the natural logarithms of all values,
            // and divide by the count of numbers and take the exponential
            return Math.Exp(values.Sum(Math.Log) / values.Count);
        }
    }
}
