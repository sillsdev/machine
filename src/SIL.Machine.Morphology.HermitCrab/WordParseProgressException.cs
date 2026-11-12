using System;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Thrown by <see cref="Morpher.ParseWords"/> when progress callbacks failed. The inner exceptions are the
    /// callback failures; <see cref="Results"/> holds every word's result in input order.
    /// </summary>
    public sealed class WordParseProgressException : AggregateException
    {
        internal WordParseProgressException(IReadOnlyList<WordParseResult> results, IEnumerable<Exception> errors)
            : base("One or more progress callbacks failed.", errors)
        {
            Results = results;
        }

        public IReadOnlyList<WordParseResult> Results { get; }
    }
}
