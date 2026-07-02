using System;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Reports whether <see cref="Morpher.MaxParseSteps"/>/<see cref="Morpher.ParseTimeout"/> cut a parse
    /// short. A breach is a soft-stop: the parse still returns whatever analyses/syntheses it had found,
    /// this just tells the caller the result may be incomplete rather than "no parse".
    /// </summary>
    public sealed class ParseDiagnostics
    {
        internal ParseDiagnostics(
            bool budgetExhausted,
            ParseExhaustionReason reason,
            int stepsUsed,
            TimeSpan elapsed,
            IReadOnlyList<(IHCRule Rule, int Applications)> topRules
        )
        {
            BudgetExhausted = budgetExhausted;
            Reason = reason;
            StepsUsed = stepsUsed;
            Elapsed = elapsed;
            TopRules = topRules ?? Array.Empty<(IHCRule Rule, int Applications)>();
        }

        public bool BudgetExhausted { get; }

        public ParseExhaustionReason Reason { get; }

        public int StepsUsed { get; }

        public TimeSpan Elapsed { get; }

        /// <summary>Populated only by <see cref="Morpher.RerunWithDiagnostics"/>.</summary>
        public IReadOnlyList<(IHCRule Rule, int Applications)> TopRules { get; }
    }
}
