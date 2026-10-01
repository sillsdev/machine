namespace SIL.Machine.Morphology.HermitCrab
{
    public enum TemplateSlotOutcome
    {
        RuleApplied,
        OptionalSkipped,
        RequiredUnfilled,
    }

    public enum PartialParseCause
    {
        RealizationalFeatureMismatch,
        UnappliedMorphologicalRules,
        NonFinalTemplateAppliedLast,
        ApplicableTemplatesNotApplied,
    }

    public class PartialParseFailure
    {
        public PartialParseFailure(PartialParseCause cause, IMorphologicalRule rule = null)
        {
            Cause = cause;
            Rule = rule;
        }

        public PartialParseCause Cause { get; }

        public IMorphologicalRule Rule { get; }
    }

    /// <summary>
    /// Optional tracing capability. ITraceManager implementations receive the existing callbacks without opting in.
    /// </summary>
    public interface IDetailedTraceManager : ITraceManager
    {
        void MorphologicalRuleNotUnapplied(
            IMorphologicalRule rule,
            int subruleIndex,
            Word input,
            FailureReason reason,
            object failureObj
        );

        /// <summary>
        /// Reports fully enumerated lookup results as yielded Word candidates, before surface validation.
        /// Interrupted lookups do not receive this callback. Each input has at most one active lookup.
        /// </summary>
        void LexicalLookupCompleted(Stratum stratum, Word input, int candidateCount, bool guessed);

        /// <summary>
        /// Reports a slot branch. OptionalSkipped can accompany RuleApplied for the same input.
        /// RequiredUnfilled means the slot produced no output; no linguistic cause is implied.
        /// </summary>
        void TemplateSlotProcessed(
            AffixTemplate template,
            int slotIndex,
            Word input,
            Word output,
            bool analysis,
            TemplateSlotOutcome outcome
        );
    }
}
