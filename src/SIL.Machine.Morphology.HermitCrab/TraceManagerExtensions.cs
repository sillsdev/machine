namespace SIL.Machine.Morphology.HermitCrab
{
    internal static class TraceManagerExtensions
    {
        /// <summary>
        /// IDetailedTraceManager receives every rejection. ITraceManager implementations receive only the Pattern
        /// rejection of a subrule, the one place a subrule ends without output.
        /// </summary>
        public static void MorphologicalRuleNotUnapplied(
            this ITraceManager traceManager,
            IMorphologicalRule rule,
            int subruleIndex,
            Word input,
            FailureReason reason,
            object failureObj
        )
        {
            if (traceManager is IDetailedTraceManager detailed)
                detailed.MorphologicalRuleNotUnapplied(rule, subruleIndex, input, reason, failureObj);
            else if (reason == FailureReason.Pattern)
                traceManager.MorphologicalRuleNotUnapplied(rule, subruleIndex, input);
        }

        /// <summary>
        /// ITraceManager implementations previously received only the productivity-restriction rejection,
        /// with no subrule index, so they keep exactly that and nothing more.
        /// </summary>
        public static void CompoundingRuleRejected(
            this ITraceManager traceManager,
            IMorphologicalRule rule,
            int subruleIndex,
            Word input,
            FailureReason reason,
            object failureObj
        )
        {
            if (traceManager is IDetailedTraceManager)
                traceManager.CompoundingRuleNotUnapplied(rule, subruleIndex, input, reason, failureObj);
            else if (reason == FailureReason.NonHeadProdRestrictMprFeatures)
                traceManager.CompoundingRuleNotUnapplied(rule, -1, input, reason, failureObj);
        }

        public static void LexicalLookupCompleted(
            this ITraceManager traceManager,
            Stratum stratum,
            Word input,
            int candidateCount,
            bool guessed
        )
        {
            if (traceManager is IDetailedTraceManager detailed)
                detailed.LexicalLookupCompleted(stratum, input, candidateCount, guessed);
        }

        public static void TemplateSlotProcessed(
            this ITraceManager traceManager,
            AffixTemplate template,
            int slotIndex,
            Word input,
            Word output,
            bool analysis,
            TemplateSlotOutcome outcome
        )
        {
            if (traceManager is IDetailedTraceManager detailed)
                detailed.TemplateSlotProcessed(template, slotIndex, input, output, analysis, outcome);
        }
    }
}
