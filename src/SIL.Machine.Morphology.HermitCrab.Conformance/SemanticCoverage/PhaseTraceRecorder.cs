#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>The three parse phases a catalog feature declares an effect in.</summary>
public enum ParsePhase
{
    /// <summary>Unapplication: proposing analyses by undoing rules.</summary>
    AnalysisCandidate,

    /// <summary>Application: confirming a candidate by regenerating its surface.</summary>
    SynthesisConfirmation,

    /// <summary>The verdict on a fully built word.</summary>
    FinalParse,
}

/// <summary>Records engine trace events bucketed by the phase that emitted them.</summary>
/// <remarks>The engine's trace interface already separates unapplication from application from the
/// final verdict, which is what makes a phase-level claim checkable rather than asserted: a feature
/// is said to act in a phase only when neutralizing it changes that phase's events.</remarks>
public sealed class PhaseTraceRecorder : ITraceManager
{
    private readonly Dictionary<ParsePhase, List<string>> _events = new()
    {
        [ParsePhase.AnalysisCandidate] = new List<string>(),
        [ParsePhase.SynthesisConfirmation] = new List<string>(),
        [ParsePhase.FinalParse] = new List<string>(),
    };

    public bool IsTracing { get; set; } = true;

    /// <summary>The recorded events of one phase, in the order the engine emitted them.</summary>
    public IReadOnlyList<string> Events(ParsePhase phase) => _events[phase];

    /// <summary>An order-insensitive digest of a phase, for comparing two runs.</summary>
    /// <remarks>Order is deliberately dropped: the engine parallelizes synthesis, so event order is
    /// not stable between runs of the same grammar and would report differences that are not
    /// semantic ones.</remarks>
    public string Digest(ParsePhase phase) =>
        string.Join(
            "\n",
            _events[phase]
                .GroupBy(item => item, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Count().ToString(CultureInfo.InvariantCulture)}x {group.Key}")
        );

    private void Record(ParsePhase phase, string kind, params string?[] parts) =>
        _events[phase]
            .Add(
                parts.Length == 0 ? kind : kind + "(" + string.Join(",", parts.Select(part => part ?? "<null>")) + ")"
            );

    private static string RuleName(IHCRule? rule) => rule?.Name ?? "<unnamed>";

    private static string Index(int value) => value.ToString(CultureInfo.InvariantCulture);

    public object GenerateWords(Language lang) => new object();

    // Analysis: every unapplication hook.
    public void AnalyzeWord(Language lang, Word input) => Record(ParsePhase.AnalysisCandidate, "AnalyzeWord");

    public void BeginUnapplyStratum(Stratum stratum, Word input) =>
        Record(ParsePhase.AnalysisCandidate, "BeginUnapplyStratum", stratum?.Name);

    public void EndUnapplyStratum(Stratum stratum, Word output) =>
        Record(ParsePhase.AnalysisCandidate, "EndUnapplyStratum", stratum?.Name);

    public void PhonologicalRuleUnapplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output) =>
        Record(ParsePhase.AnalysisCandidate, "PhonologicalRuleUnapplied", RuleName(rule), Index(subruleIndex));

    public void PhonologicalRuleNotUnapplied(IPhonologicalRule rule, int subruleIndex, Word input) =>
        Record(ParsePhase.AnalysisCandidate, "PhonologicalRuleNotUnapplied", RuleName(rule), Index(subruleIndex));

    public void BeginUnapplyTemplate(AffixTemplate template, Word input) =>
        Record(ParsePhase.AnalysisCandidate, "BeginUnapplyTemplate", template?.Name);

    public void EndUnapplyTemplate(AffixTemplate template, Word output, bool unapplied) =>
        Record(ParsePhase.AnalysisCandidate, "EndUnapplyTemplate", template?.Name, unapplied.ToString());

    public void MorphologicalRuleUnapplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output) =>
        Record(ParsePhase.AnalysisCandidate, "MorphologicalRuleUnapplied", RuleName(rule), Index(subruleIndex));

    public void MorphologicalRuleNotUnapplied(IMorphologicalRule rule, int subruleIndex, Word input) =>
        Record(ParsePhase.AnalysisCandidate, "MorphologicalRuleNotUnapplied", RuleName(rule), Index(subruleIndex));

    public void CompoundingRuleNotUnapplied(
        IMorphologicalRule rule,
        int subruleIndex,
        Word input,
        FailureReason reason,
        object failureObj
    ) =>
        Record(
            ParsePhase.AnalysisCandidate,
            "CompoundingRuleNotUnapplied",
            RuleName(rule),
            Index(subruleIndex),
            reason.ToString()
        );

    // The hinge, and the engine puts it on the synthesis side: Morpher.Synthesize calls it per
    // analysis to find the entries a rebuild can start from.
    public void LexicalLookup(Stratum stratum, Word input) =>
        Record(ParsePhase.SynthesisConfirmation, "LexicalLookup", stratum?.Name);

    // Synthesis: every application hook.
    public void SynthesizeWord(Language lang, Word input) => Record(ParsePhase.SynthesisConfirmation, "SynthesizeWord");

    public void BeginApplyStratum(Stratum stratum, Word input) =>
        Record(ParsePhase.SynthesisConfirmation, "BeginApplyStratum", stratum?.Name);

    public void NonFinalTemplateAppliedLast(Stratum stratum, Word word) =>
        Record(ParsePhase.SynthesisConfirmation, "NonFinalTemplateAppliedLast", stratum?.Name);

    public void ApplicableTemplatesNotApplied(Stratum stratum, Word word) =>
        Record(ParsePhase.SynthesisConfirmation, "ApplicableTemplatesNotApplied", stratum?.Name);

    public void EndApplyStratum(Stratum stratum, Word output) =>
        Record(ParsePhase.SynthesisConfirmation, "EndApplyStratum", stratum?.Name);

    public void PhonologicalRuleApplied(IPhonologicalRule rule, int subruleIndex, Word input, Word output) =>
        Record(ParsePhase.SynthesisConfirmation, "PhonologicalRuleApplied", RuleName(rule), Index(subruleIndex));

    public void PhonologicalRuleNotApplied(
        IPhonologicalRule rule,
        int subruleIndex,
        Word input,
        FailureReason reason,
        object failureObj
    ) =>
        Record(
            ParsePhase.SynthesisConfirmation,
            "PhonologicalRuleNotApplied",
            RuleName(rule),
            Index(subruleIndex),
            reason.ToString()
        );

    public void BeginApplyTemplate(AffixTemplate template, Word input) =>
        Record(ParsePhase.SynthesisConfirmation, "BeginApplyTemplate", template?.Name);

    public void EndApplyTemplate(AffixTemplate template, Word output, bool applied) =>
        Record(ParsePhase.SynthesisConfirmation, "EndApplyTemplate", template?.Name, applied.ToString());

    public void MorphologicalRuleApplied(IMorphologicalRule rule, int subruleIndex, Word input, Word output) =>
        Record(ParsePhase.SynthesisConfirmation, "MorphologicalRuleApplied", RuleName(rule), Index(subruleIndex));

    public void MorphologicalRuleNotApplied(
        IMorphologicalRule rule,
        int subruleIndex,
        Word input,
        FailureReason reason,
        object failureObj
    ) =>
        Record(
            ParsePhase.SynthesisConfirmation,
            "MorphologicalRuleNotApplied",
            RuleName(rule),
            Index(subruleIndex),
            reason.ToString()
        );

    public void CompoundingRuleNotApplied(
        IMorphologicalRule rule,
        int subruleIndex,
        Word input,
        FailureReason reason,
        object failureObj
    ) =>
        Record(
            ParsePhase.SynthesisConfirmation,
            "CompoundingRuleNotApplied",
            RuleName(rule),
            Index(subruleIndex),
            reason.ToString()
        );

    // Final parse: the verdict on a completed word.
    public void Blocked(IHCRule rule, Word output) => Record(ParsePhase.FinalParse, "Blocked", RuleName(rule));

    public void Successful(Language lang, Word word) => Record(ParsePhase.FinalParse, "Successful");

    public void Failed(Language lang, Word word, FailureReason reason, Allomorph allomorph, object failureObj) =>
        Record(ParsePhase.FinalParse, "Failed", reason.ToString());
}
