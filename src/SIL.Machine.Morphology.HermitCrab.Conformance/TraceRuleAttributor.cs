using System;
using System.Collections.Generic;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Extracts, for one successful parse (<see cref="Word"/>) returned by <c>Morpher.ParseWord</c>,
/// the set of grammar rule ids the plan's "rules:" field is verified against, per
/// docs/conformance-language-suite-plan.md section 2.1 ("self-check runs the oracle with tracing
/// enabled and FAILs if the traced rule applications don't match the declared list").
///
/// <b>Two different sources, because no single API gives both:</b>
/// <list type="bullet">
/// <item><b>Morphological rules</b> (AffixProcessRule/CompoundingRule) come straight off
/// <see cref="Word.MorphologicalRules"/> -- populated once during analysis-direction unapplication
/// and carried forward unchanged through synthesis, so it is exact and per-result, no ambiguity.</item>
/// <item><b>Phonological rules</b> (RewriteRule/MetathesisRule) have no equivalent per-Word list --
/// <see cref="Morpher"/> only records their application as <see cref="Trace"/> nodes
/// (<see cref="TraceType.PhonologicalRuleSynthesis"/>), via <see cref="ITraceManager.PhonologicalRuleApplied"/>.
/// Unlike <see cref="ITraceManager.MorphologicalRuleApplied"/>, that call does NOT update
/// <c>Word.CurrentTrace</c> (see TraceManager.cs), so a phonological-rule trace node is a *sibling*
/// of the branch that produced a given final Word, not an ancestor reachable by walking
/// <c>CurrentTrace</c> upward. This class instead walks the ENTIRE trace tree for the word (the
/// <c>trace</c> out-parameter of <c>ParseWord</c>) and collects every
/// <see cref="TraceType.PhonologicalRuleSynthesis"/> node system-wide.</item>
/// </list>
///
/// <b>Documented limitation (ambiguous words):</b> when a surface word has more than one successful
/// analysis, this global trace walk cannot attribute a specific phonological rule firing to one
/// analysis branch over another -- it reports the UNION of every phonological rule that fired
/// anywhere while producing any of the word's results. <see cref="Runner"/> applies this same
/// phonological-rule set to every one of that word's declared parses, so a false PASS is possible
/// in principle for an ambiguous word whose two parses use different phonological rules but the
/// same declared "rules:" set does not distinguish them. This does not affect any G1 fixture (the
/// pilot grammar has zero phonological rules by construction; the one ambiguous pilot word's
/// parses differ only in a morphological rule, which IS attributed exactly per-result). A future
/// language with both ambiguity and phonological rules would need per-branch trace correlation
/// (e.g. threading a distinguishing marker through <c>ITraceManager.PhonologicalRuleApplied</c>) to
/// close this gap -- flagged here rather than silently shipped.
///
/// <b>RealizationalAffixProcessRule applications are attributed word-level too:</b>
/// <c>Word.MorphologicalRuleUnapplied</c> deliberately excludes them from the <c>_mruleApps</c>
/// list that backs <see cref="Word.MorphologicalRules"/> (see Word.cs), so they cannot come from
/// the per-result source above -- but <c>SynthesisRealizationalAffixProcessRule</c> does report
/// successful applications through <see cref="ITraceManager.MorphologicalRuleApplied"/>, which
/// records a <see cref="TraceType.MorphologicalRuleSynthesis"/> node. The same trace walk that
/// collects phonological rules therefore also collects realizational rules (with the same
/// union-across-results ambiguity caveat), keyed off the node's Source type.
/// </summary>
public static class TraceRuleAttributor
{
    public static HashSet<string> MorphologicalRuleIds(Word result, GrammarRuleIndex index)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (IMorphologicalRule rule in result.MorphologicalRules)
        {
            string id = index.ResolveMorphologicalRuleId(rule);
            if (id != null)
                ids.Add(id);
        }
        return ids;
    }

    /// <summary>The word-level (not per-result) set of phonological and realizational rule ids fired anywhere in the trace tree rooted at <paramref name="trace"/> -- see the class doc comment for why these two kinds are word-level, not per-result.</summary>
    public static HashSet<string> WordLevelRuleIds(object trace, GrammarRuleIndex index)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (trace is not Trace root)
            return ids;
        Walk(root);
        return ids;

        void Walk(Trace node)
        {
            // TraceManager records BOTH outcomes of a synthesis-direction rule attempt as
            // *RuleSynthesis nodes: *RuleApplied (FailureReason.None, Output set) and
            // *RuleNotApplied (FailureReason set, no Output). Only the applied nodes count as
            // "this rule fired" -- without this filter, every phonological rule in a stratum shows
            // up in every word's traced set merely because its pass ran.
            if (node.FailureReason == FailureReason.None)
            {
                if (node.Type == TraceType.PhonologicalRuleSynthesis && node.Source is IPhonologicalRule prule)
                {
                    string id = index.ResolvePhonologicalRuleId(prule);
                    if (id != null)
                        ids.Add(id);
                }
                else if (
                    node.Type == TraceType.MorphologicalRuleSynthesis
                    && node.Source is RealizationalAffixProcessRule rrule
                )
                {
                    string id = index.ResolveMorphologicalRuleId(rrule);
                    if (id != null)
                        ids.Add(id);
                }
            }
            foreach (Trace child in node.Children)
                Walk(child);
        }
    }
}
