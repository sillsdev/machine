namespace SIL.Machine.Morphology.HermitCrab
{
    // Represent the state of a final template in analysis.
    // This could be extended to represent the state of a final template in synthesis
    // (replacing or implementing IsLastAppliedRuleFinal using None, FinalTemplate, NonFinalTemplate).
    enum FinalTemplateState : byte
    {
        None,
        NonTemplate,
    }
}
