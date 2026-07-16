using System;

namespace SIL.Machine.Rules
{
    public class MaxAlternativesExceededException : Exception
    {
        public MaxAlternativesExceededException() { }

        public MaxAlternativesExceededException(string message)
            : base(message) { }

        public MaxAlternativesExceededException(string message, Exception inner)
            : base(message, inner) { }
    }
}
