using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// One finding from <see cref="GrammarAnalyzer.Analyze"/>: a static "don't do this" signal about a
    /// specific rule shape, keyed by a stable <see cref="Code"/> so other tools (FLEx's parser report,
    /// a CLI) can key documentation/UI off it. See complexity-cap.md §6 for the code catalogue and the
    /// "Writing performant HC grammars" guide organized by these codes.
    /// </summary>
    public sealed class GrammarDiagnostic
    {
        internal GrammarDiagnostic(
            string code,
            DiagnosticSeverity severity,
            object rule,
            string message,
            string suggestion
        )
        {
            Code = code;
            Severity = severity;
            Rule = rule;
            Message = message;
            Suggestion = suggestion;
        }

        public string Code { get; }
        public DiagnosticSeverity Severity { get; }

        /// <summary>The culprit object — an <see cref="IHCRule"/> (rule/template) or a <see cref="Morpheme"/> (lexical entry).</summary>
        public object Rule { get; }
        public string Message { get; }
        public string Suggestion { get; }

        public override string ToString()
        {
            string ruleName = (Rule as IHCRule)?.Name ?? (Rule as Morpheme)?.Id ?? Rule?.ToString();
            return $"{Code} [{Severity}] {ruleName}: {Message}";
        }
    }

    /// <summary>
    /// Layer 3 of complexity-cap.md: static analysis over a loaded <see cref="Language"/> that flags
    /// rule shapes which are always-wrong or almost-always-wrong for parse complexity — independent of
    /// any specific word, and independent of whether the grammar was loaded from XML or built
    /// programmatically (FieldWorks' HCLoader), since both produce the same in-memory <see cref="Language"/>.
    /// What this *cannot* catch is combinatorial interaction between individually-reasonable rules; that
    /// is covered empirically by <see cref="Morpher.RerunWithDiagnostics"/> instead (see complexity-cap.md §6.2).
    /// </summary>
    public static class GrammarAnalyzer
    {
        public static IReadOnlyList<GrammarDiagnostic> Analyze(Language language)
        {
            var diagnostics = new List<GrammarDiagnostic>();
            foreach (Stratum stratum in language.Strata)
            {
                foreach (IMorphologicalRule rule in stratum.MorphologicalRules)
                {
                    if (rule is AffixProcessRule affixRule)
                        CheckAffixProcessRule(affixRule, diagnostics);
                    else if (rule is CompoundingRule compoundingRule)
                        CheckCompoundingRule(compoundingRule, diagnostics);
                }

                foreach (IPhonologicalRule prule in stratum.PhonologicalRules)
                {
                    if (prule is RewriteRule rewriteRule)
                        CheckRewriteRule(rewriteRule, diagnostics);
                }

                CheckLexicalPatterns(stratum, diagnostics);
            }

            CheckCyclicFeedingPairs(language, diagnostics);

            return diagnostics;
        }

        // HC0001 / HC0002 / HC0003
        private static void CheckAffixProcessRule(AffixProcessRule rule, List<GrammarDiagnostic> diagnostics)
        {
            if (HasNoOvertExponent(rule))
            {
                if (rule.MaxApplicationCount > 1)
                {
                    diagnostics.Add(
                        new GrammarDiagnostic(
                            "HC0001",
                            DiagnosticSeverity.Error,
                            rule,
                            "Affix rule has no overt exponent (every allomorph's output is a pure copy of "
                                + "the input, adding no phonological material) and MaxApplicationCount > 1. "
                                + "This unapplies to every word, every time, with no way to ever stop: "
                                + "guaranteed exponential.",
                            "Give the rule an overt exponent, or set MaxApplicationCount back to 1."
                        )
                    );
                }
                else
                {
                    diagnostics.Add(
                        new GrammarDiagnostic(
                            "HC0002",
                            DiagnosticSeverity.Warning,
                            rule,
                            "Affix rule has no overt exponent (every allomorph's output is a pure copy of "
                                + "the input, adding no phonological material). It still multiplies "
                                + "candidates once per cascade position and is frequently unintended.",
                            "Add an overt exponent (an inserted segment/boundary), or confirm this "
                                + "zero-exponent rule (e.g. a purely feature-changing rule) is intentional."
                        )
                    );
                }
            }

            if (rule.MaxApplicationCount > 1)
            {
                diagnostics.Add(
                    new GrammarDiagnostic(
                        "HC0003",
                        DiagnosticSeverity.Warning,
                        rule,
                        $"MaxApplicationCount is {rule.MaxApplicationCount} (the XML multipleApplication "
                            + "attribute raises it above the default of 1) — this is precisely where an "
                            + "unbounded grammar opts into unboundedness.",
                        "Confirm a bound this high is actually needed; prefer the smallest value that "
                            + "covers legitimate words."
                    )
                );
            }
        }

        private static bool HasNoOvertExponent(AffixProcessRule rule)
        {
            if (rule.Allomorphs.Count == 0)
                return false;
            return rule.Allomorphs.All(allo =>
                allo.Rhs.All(action => action is CopyFromInput || action is ModifyFromInput)
            );
        }

        // HC0006
        private static void CheckCompoundingRule(CompoundingRule rule, List<GrammarDiagnostic> diagnostics)
        {
            if (rule.HeadRequiredSyntacticFeatureStruct.IsEmpty && rule.NonHeadRequiredSyntacticFeatureStruct.IsEmpty)
            {
                diagnostics.Add(
                    new GrammarDiagnostic(
                        "HC0006",
                        DiagnosticSeverity.Warning,
                        rule,
                        "Compounding rule constrains the part of speech of neither the head nor the "
                            + "non-head — every stem in the lexicon is a candidate on both sides, a "
                            + "cross-product blowup that interacts with Morpher.MaxStemCount.",
                        "Constrain HeadRequiredSyntacticFeatureStruct and/or "
                            + "NonHeadRequiredSyntacticFeatureStruct to the parts of speech that can "
                            + "actually compound."
                    )
                );
            }
        }

        // HC0004 / HC0005
        private static void CheckRewriteRule(RewriteRule rule, List<GrammarDiagnostic> diagnostics)
        {
            foreach (RewriteSubrule subrule in rule.Subrules)
            {
                // Deletion subrule: underlying (Lhs) longer than surface (Rhs) — synthesis deletes
                // material, so analysis must hypothesize/reinsert it. Matches AnalysisRewriteRule's own
                // ReapplyType.Deletion classification.
                if (rule.Lhs.Children.Count > subrule.Rhs.Children.Count)
                {
                    if (subrule.LeftEnvironment.Children.Count == 0 && subrule.RightEnvironment.Children.Count == 0)
                    {
                        diagnostics.Add(
                            new GrammarDiagnostic(
                                "HC0005",
                                DiagnosticSeverity.Warning,
                                rule,
                                "Deletion rule has no left or right environment constraint at all — "
                                    + "analysis can hypothesize a deleted segment matching this pattern "
                                    + "anywhere in the word, unboundedly reinserting it (interacts with "
                                    + "Morpher.DeletionReapplications).",
                                "Add a left and/or right environment constraint so reinsertion is only "
                                    + "considered in the position(s) where deletion could plausibly have occurred."
                            )
                        );
                    }
                }

                // Self-feeding: matches AnalysisRewriteRule's own ReapplyType.SelfOpaquing selection
                // exactly — that path had no reapplication bound at all before complexity-cap Layer 1,
                // i.e. an unconditional infinite loop for any grammar that hits it. Two distinct engine
                // branches select it (see AnalysisRewriteRule's constructor):
                //   - Lhs.Count == Rhs.Count (a same-length/feature-changing subrule): only when
                //     Simultaneous *and* a Rhs segment constraint could satisfy its own environment again.
                //   - Lhs.Count == 0 (epenthesis): unconditionally, whenever Simultaneous — the inserted
                //     segment's own shape is irrelevant, so there's no unification check to gate it.
                bool isSelfOpaquing;
                if (rule.Lhs.Children.Count == subrule.Rhs.Children.Count)
                {
                    isSelfOpaquing =
                        rule.ApplicationMode == RewriteApplicationMode.Simultaneous && IsSelfFeeding(subrule);
                }
                else if (rule.Lhs.Children.Count == 0)
                {
                    isSelfOpaquing = rule.ApplicationMode == RewriteApplicationMode.Simultaneous;
                }
                else
                {
                    isSelfOpaquing = false; // Deletion/expansion branches — always ReapplyType.Deletion.
                }

                if (isSelfOpaquing)
                {
                    diagnostics.Add(
                        new GrammarDiagnostic(
                            "HC0004",
                            DiagnosticSeverity.Warning,
                            rule,
                            "Simultaneous-mode rewrite rule whose output can satisfy its own environment "
                                + "again (self-feeding) — analysis can keep re-hypothesizing this rule's "
                                + "effect on its own output indefinitely.",
                            "Add an environment constraint that excludes the rule's own output, or switch "
                                + "to Iterative application mode if that's the intent."
                        )
                    );
                }
            }
        }

        private static bool IsSelfFeeding(RewriteSubrule subrule)
        {
            foreach (Constraint<Word, int> constraint in subrule.Rhs.Children.OfType<Constraint<Word, int>>())
            {
                if (constraint.Type() != HCFeatureSystem.Segment)
                    continue;
                if (
                    !constraint.IsUnifiableWithEnvironment(subrule.LeftEnvironment)
                    || !constraint.IsUnifiableWithEnvironment(subrule.RightEnvironment)
                )
                {
                    return true;
                }
            }
            return false;
        }

        // HC0007
        private static void CheckLexicalPatterns(Stratum stratum, List<GrammarDiagnostic> diagnostics)
        {
            foreach (LexEntry entry in stratum.Entries)
            {
                foreach (RootAllomorph allomorph in entry.Allomorphs)
                {
                    if (!allomorph.IsPattern)
                        continue;
                    int consecutiveOptional = 0;
                    bool flagged = false;
                    foreach (ShapeNode node in allomorph.Segments.Shape)
                    {
                        if (flagged)
                            break;
                        if (node.Annotation.Optional || node.IsIterative())
                        {
                            consecutiveOptional++;
                            if (consecutiveOptional >= 2)
                            {
                                diagnostics.Add(
                                    new GrammarDiagnostic(
                                        "HC0007",
                                        DiagnosticSeverity.Info,
                                        entry,
                                        $"Lexical pattern '{entry.Id}' has two or more adjacent "
                                            + "optional/iterative segments — a known source of spurious "
                                            + "ambiguity (multiple paths through the pattern produce the "
                                            + "same string).",
                                        "Prefer a single Kleene-star class over back-to-back optional groups."
                                    )
                                );
                                flagged = true;
                            }
                        }
                        else
                        {
                            consecutiveOptional = 0;
                        }
                    }
                }
            }
        }

        // HC0008
        private static void CheckCyclicFeedingPairs(Language language, List<GrammarDiagnostic> diagnostics)
        {
            foreach (Stratum stratum in language.Strata)
            {
                List<AffixProcessRule> rules = stratum.MorphologicalRules.OfType<AffixProcessRule>().ToList();
                for (int i = 0; i < rules.Count; i++)
                {
                    for (int j = i + 1; j < rules.Count; j++)
                    {
                        AffixProcessRule a = rules[i];
                        AffixProcessRule b = rules[j];
                        // Best-effort, high-confidence-only pairs (per complexity-cap.md §10 open
                        // question #6): both sides add no overt exponent, and each rule's output
                        // syntactic category is compatible with the other's input requirement — an
                        // A-then-B-then-A-then-B chain that never terminates via shape change.
                        if (
                            HasNoOvertExponent(a)
                            && HasNoOvertExponent(b)
                            && a.OutSyntacticFeatureStruct.IsUnifiable(b.RequiredSyntacticFeatureStruct)
                            && b.OutSyntacticFeatureStruct.IsUnifiable(a.RequiredSyntacticFeatureStruct)
                        )
                        {
                            diagnostics.Add(
                                new GrammarDiagnostic(
                                    "HC0008",
                                    DiagnosticSeverity.Info,
                                    a,
                                    $"'{a.Name}' and '{b.Name}' both add no overt exponent and each "
                                        + "rule's output category is compatible with the other's input "
                                        + "requirement — a cyclic feeding pair (A feeds B feeds A) is "
                                        + "structurally possible.",
                                    "Verify these two rules can't unapply to each other indefinitely; "
                                        + "consider a MaxRuleApplicationsPerWord cap either way."
                                )
                            );
                        }
                    }
                }
            }
        }
    }
}
