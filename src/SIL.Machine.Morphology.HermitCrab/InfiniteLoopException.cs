using System;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// This exception is thrown when a rule is caught in an infinite loop.
    /// </summary>
    public class InfiniteLoopException : Exception
    {
        public InfiniteLoopException(string message)
            : this(message, null) { }

        public InfiniteLoopException(string message, string ruleName)
            : base(message)
        {
            RuleName = ruleName;
        }

        /// <summary>The looping rule's <see cref="IHCRule.Name"/> (grammar.xml's <c>&lt;Name&gt;</c>
        /// child text, e.g. "rule4"), or null when the throw site could not identify one. No
        /// HermitCrab runtime rule object retains the XML <c>id</c> attribute itself, only this Name;
        /// callers that need the grammar.xml id translate it back the same way a traced rule
        /// application already does.</summary>
        public string RuleName { get; }
    }
}
