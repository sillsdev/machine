using System;
using System.Collections.Generic;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Failure-side counterpart to <see cref="TraceRuleAttributor"/>: for one EXPECT_FAIL word already
/// confirmed elsewhere (self-check's own signature comparison) to have zero total parses, extracts
/// the grammar rule ids a real engine trace shows were EVALUATED AND REJECTED -- or blocked after
/// applying -- while reaching that result. This upgrades a hand-written <c>blocked_by</c> label from
/// asserted to VERIFIED, the same standard a crash-attributed rule is held to elsewhere in the
/// coverage report.
///
/// <b>Two trace shapes are used, both inherently self-limiting rather than filtered by
/// <see cref="FailureReason"/> value:</b>
/// <list type="bullet">
/// <item>A <see cref="TraceType.Blocked"/> node (<c>Word.CheckBlocking</c> substituted a suppletive
/// family partner) can only ever appear AFTER the rule already applied successfully to build that
/// specific candidate -- it cannot fire for a rule that was never in scope for this word, so it
/// carries rule identity directly on <see cref="Trace.Source"/> with no risk of blanket noise.</item>
/// <item>A <see cref="TraceType.Failed"/> node whose <see cref="Trace.FailureAllomorph"/> resolves to
/// an <see cref="AffixProcessAllomorph"/> is likewise self-limiting: <c>Environments</c>/
/// <c>AllomorphCoOccurrenceRules</c>/<c>MorphemeCoOccurrenceRules</c> are checked in
/// <c>Morpher.IsWordValid</c> only against allomorphs that are ALREADY PART OF a fully-synthesized
/// final candidate, so the rule that owns the allomorph (<see cref="Allomorph.Morpheme"/> -- an
/// <see cref="AffixProcessRule"/>/<see cref="RealizationalAffixProcessRule"/> is itself a
/// <see cref="Morpheme"/>) genuinely did apply to build that candidate, not merely get tried.</item>
/// </list>
///
/// <b>Deliberately NOT used: a <see cref="TraceType.MorphologicalRuleSynthesis"/>/
/// <see cref="TraceType.CompoundingRuleSynthesis"/>/<see cref="TraceType.PhonologicalRuleSynthesis"/>
/// NotApplied node's <see cref="Trace.FailureReason"/>, for ANY reason value, not just the
/// MPR-feature family.</b> Confirmed empirically across this suite's own fixtures: a rule reached at
/// all during a linear/unordered stratum's search gets tried against candidates it has nothing to do
/// with, and <see cref="FailureReason.RequiredMprFeatures"/>/<see cref="FailureReason.RequiredSyntacticFeatureStruct"/>
/// alike fire routinely in that "not in scope for this candidate" sense (ordinary
/// <c>requiredPartsOfSpeech</c> gating on a phonological subrule compiles to the same
/// RequiredSyntacticFeatureStruct reason a genuine per-word feature CONFLICT does) -- there is no
/// FailureReason value that reliably separates "this candidate specifically conflicts with the rule"
/// from "the rule doesn't apply here, as it structurally never would." Widening on either produced
/// real, wrong cross-fixture attributions in testing (e.g. an unrelated word crediting a phonological
/// rule several constructs away). One target rule's one word (rrRRealTest/zimed, a genuine
/// RequiredSyntacticFeatureStruct conflict) is therefore left label-only rather than verified this
/// way -- see the class's own investigation notes for why: the rule ITSELF still verifies (its
/// sibling word zamed hits the Blocked case above), so the dead-rule/label-only gate is unaffected,
/// but this one word's own claim stays unverified. Under-attributing here is the safe direction.
///
/// <b>A phonological rule's own <see cref="FailureReason.SurfaceFormMismatch"/> is reconstructed,
/// not read off one node:</b> the rule that mutates a confirming-synthesis candidate away from the
/// input surface fires as an ordinary Applied node (the RULE succeeded; it is the WORD that then
/// fails to match) -- so an Applied <see cref="TraceType.PhonologicalRuleSynthesis"/> node is only
/// attributed when the SAME WORD's own trace tree (a fresh <c>ParseWord</c> call per word, never
/// shared) also contains a top-level <see cref="TraceType.Failed"/> node carrying
/// <see cref="FailureReason.SurfaceFormMismatch"/>, tying the rule to the specific failure mode its
/// own mechanism produces.
///
/// <b>What this cannot reach at all:</b> <see cref="FailureReason.AllomorphCoOccurrenceRules"/> and
/// <see cref="FailureReason.MorphemeCoOccurrenceRules"/> are raised against a <see cref="RootAllomorph"/>
/// -- a lexical entry's own allomorph, e.g. a free-variation pair sharing one <see cref="Morpheme"/>
/// -- whose <c>Morpheme</c> identifies only the shared morpheme, not which of its several allomorphs
/// failed; the runtime <see cref="Allomorph"/> object never retains its grammar.xml <c>id</c>
/// attribute at all (unlike <see cref="Morpheme.Id"/>). No verified channel exists for that case
/// without adding the missing id -- see <see cref="GrammarRuleIndex"/>'s own doc comment for the
/// analogous, already-documented loss for four other rule kinds.
/// </summary>
public static class FailureRuleAttributor
{
    public static HashSet<string> WordLevelFailureRuleIds(object trace, GrammarRuleIndex index)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (trace is not Trace root)
            return ids;

        bool surfaceFormMismatch = HasSurfaceFormMismatch(root);
        Walk(root);
        return ids;

        void Walk(Trace node)
        {
            switch (node.Type)
            {
                case TraceType.Blocked:
                    AddMorphological(node.Source);
                    break;
                case TraceType.PhonologicalRuleSynthesis:
                    if (node.FailureReason == FailureReason.None && surfaceFormMismatch)
                        AddPhonological(node.Source);
                    break;
                case TraceType.Failed:
                    if (
                        node.FailureAllomorph is AffixProcessAllomorph affixAllo
                        && affixAllo.Morpheme is IMorphologicalRule mrule
                    )
                    {
                        AddMorphological(mrule);
                    }
                    break;
            }
            foreach (Trace child in node.Children)
                Walk(child);
        }

        void AddMorphological(IHCRule source)
        {
            if (source is not IMorphologicalRule mrule)
                return;
            string id = index.ResolveMorphologicalRuleId(mrule);
            if (id != null)
                ids.Add(id);
        }

        void AddPhonological(IHCRule source)
        {
            if (source is not IPhonologicalRule prule)
                return;
            string id = index.ResolvePhonologicalRuleId(prule);
            if (id != null)
                ids.Add(id);
        }
    }

    private static bool HasSurfaceFormMismatch(Trace node)
    {
        if (node.Type == TraceType.Failed && node.FailureReason == FailureReason.SurfaceFormMismatch)
            return true;
        foreach (Trace child in node.Children)
        {
            if (HasSurfaceFormMismatch(child))
                return true;
        }
        return false;
    }
}
