#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Builds and re-verifies a <see cref="Proof"/> of kind <see cref="Kind"/>: two rules whose part-of-speech
/// gates are mutually exclusive, so no root can ever satisfy both. Two shapes are modeled:
///
/// - A <c>MorphologicalRule</c>/<c>MorphologicalRule</c> pair, on <c>requiredPartsOfSpeech</c> directly --
///   the DTD's own IDREF cardinality (<c>LexicalEntry@partOfSpeech</c> is a single IDREF) means one root
///   carries exactly one part of speech.
/// - A <c>PhonologicalRule</c>/<c>PhonologicalRule</c> pair, on the union of each rule's
///   <c>PhonologicalSubrule@requiredPartsOfSpeech</c> values -- a rule's active subrules are alternatives,
///   so the rule's own POS gate is whichever of them can fire.
///
/// This is the dangerous kind: POS-disjointness only rules out interaction when nothing in the grammar
/// can convert one part of speech into the other before the second rule is reached. This check is
/// therefore conservative by construction -- see this file's bridging step and remarks below for exactly
/// what it can and cannot see.
///
/// NOT MODELED, all failing closed to <see cref="PosRelation.Undetermined"/>: <c>RealizationalRule</c>
/// (no <c>requiredPartsOfSpeech</c> attribute at all -- it applies regardless of POS),
/// <c>CompoundingRule</c> (a <c>headPartsOfSpeech</c>/<c>nonHeadPartsOfSpeech</c> pair, a different shape
/// entirely), and <c>MetathesisRule</c> (no <c>requiredPartsOfSpeech</c> at all). A rule with no active
/// subrule declaring a restriction -- including a rule with zero active subrules -- is UNRESTRICTED and
/// can never be proven disjoint from anything; that inversion (treating "no restriction" as "disjoint from
/// everything") is the one this check must never make.
///
/// BRIDGING, checked but incomplete: any active <c>MorphologicalRule</c> or <c>CompoundingRule</c>
/// anywhere in the document (not only the owning Stratum, which is the safe direction -- more
/// Undetermined, never a missed bridge within scope) whose <c>outputPartOfSpeech</c> reaches either
/// compared rule's required set is treated as a possible bridge unless it is a MorphologicalRule
/// with a nonempty required set wholly contained in the same disjoint side as its output. Such a
/// rule preserves its own side and cannot bridge; a rule that can accept the opposite side, has an
/// unrestricted or otherwise unclassified input gate, or is a CompoundingRule remains a blocker.
/// This is a single hop: it does not check whether that bridging rule's OWN <c>requiredPartsOfSpeech</c>
/// is itself reachable from any root, so a bridge that is itself unreachable still blocks the proof --
/// safe (it can only under-prove, never over-prove) but not exhaustive. It also cannot see a bridge built
/// from <c>RequiredHeadFeatures</c>/<c>RequiredFootFeatures</c> or any output the loader derives without
/// setting <c>outputPartOfSpeech</c> textually, since no other conversion channel is inspected.
/// </summary>
public static class PosDisjointProofs
{
    public const string Kind = "pos-disjoint";

    public static Proof? TryBuild(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);
        PosDisjointCheck check = Check(grammar, item);
        return check.Relation == PosRelation.Disjoint ? new Proof(item.Id, Kind, check.Reason) : null;
    }

    /// <summary>
    /// Re-verifies <paramref name="proof"/>: true only when <paramref name="fixtureId"/>'s freshly
    /// generated adjacent pairs still contain an item with <paramref name="proof"/>'s id AND the check
    /// recomputes <see cref="PosRelation.Disjoint"/> for it. A stale id, a recomputed overlap, and a
    /// recomputed bridge all fail closed to false.
    /// </summary>
    public static bool Verify(XDocument grammar, string fixtureId, Proof proof)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(fixtureId);
        ArgumentNullException.ThrowIfNull(proof);
        OrderingItem? item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, fixtureId)
            .FirstOrDefault(candidate => candidate.Id == proof.ItemId);
        if (item is null)
            return false;
        return Check(grammar, item).Relation == PosRelation.Disjoint;
    }

    private enum PosRelation
    {
        Disjoint,
        Overlaps,
        Undetermined,
    }

    private sealed record PosDisjointCheck(string ItemId, PosRelation Relation, string Reason);

    private static PosDisjointCheck Check(XDocument grammar, OrderingItem item) =>
        item.Kind switch
        {
            OrderingListKind.StratumMorphologicalRules => CheckMorphologicalPair(grammar, item),
            OrderingListKind.StratumPhonologicalRules => CheckPhonologicalPair(grammar, item),
            _ => new PosDisjointCheck(
                item.Id,
                PosRelation.Undetermined,
                $"{item.Kind} pairs are not modeled -- only Stratum morphologicalRules/phonologicalRules pairs "
                    + "resolve to a requiredPartsOfSpeech comparison"
            ),
        };

    private static PosDisjointCheck CheckMorphologicalPair(XDocument grammar, OrderingItem item)
    {
        XElement? ruleA = FindElementById(grammar, item.MemberA);
        XElement? ruleB = FindElementById(grammar, item.MemberB);
        if (ruleA is null || ruleB is null)
        {
            string missing = ruleA is null ? item.MemberA : item.MemberB;
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Undetermined,
                $"'{missing}' was not found as an element with a matching id attribute"
            );
        }
        if (ruleA.Name.LocalName != "MorphologicalRule" || ruleB.Name.LocalName != "MorphologicalRule")
        {
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Undetermined,
                $"<{ruleA.Name.LocalName}>/<{ruleB.Name.LocalName}> is not modeled -- only a MorphologicalRule/MorphologicalRule "
                    + "pair has a plain requiredPartsOfSpeech to compare (RealizationalRule has none; CompoundingRule uses a "
                    + "different head/nonhead shape)"
            );
        }

        HashSet<string>? reqA = RequiredPartsOfSpeech(ruleA);
        HashSet<string>? reqB = RequiredPartsOfSpeech(ruleB);
        if (reqA is null || reqA.Count == 0 || reqB is null || reqB.Count == 0)
        {
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Overlaps,
                $"'{(reqA is null || reqA.Count == 0 ? item.MemberA : item.MemberB)}' declares no requiredPartsOfSpeech, "
                    + "so it applies regardless of part of speech"
            );
        }

        string[] shared = reqA.Intersect(reqB, StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        if (shared.Length > 0)
        {
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Overlaps,
                $"requiredPartsOfSpeech share {{{string.Join(",", shared)}}}"
            );
        }

        HashSet<string> union = reqA.Union(reqB, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        XElement? bridge = FindBridgingRule(grammar, union, reqA, reqB);
        if (bridge is not null)
        {
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Undetermined,
                $"possible bridge: <{bridge.Name.LocalName}> '{(string?)bridge.Attribute("id")}' outputs a part of speech "
                    + $"in {{{string.Join(",", union.OrderBy(s => s, StringComparer.Ordinal))}}}"
            );
        }

        return new PosDisjointCheck(
            item.Id,
            PosRelation.Disjoint,
            $"'{item.MemberA}' requires {{{string.Join(",", reqA.OrderBy(s => s, StringComparer.Ordinal))}}}, "
                + $"'{item.MemberB}' requires {{{string.Join(",", reqB.OrderBy(s => s, StringComparer.Ordinal))}}}, disjoint, "
                + "and no modeled active MorphologicalRule/CompoundingRule can bridge the compared part-of-speech sets "
                + "(same-side MorphologicalRule preservation is excluded)"
        );
    }

    // A PhonologicalRule's own POS gate is not on the rule element -- it is the union of whichever active
    // PhonologicalSubrule can fire, since the subrules are alternatives. A subrule with no
    // requiredPartsOfSpeech fires regardless of POS, which makes the WHOLE rule unrestricted; that is why
    // this returns Unrestricted rather than an empty set the caller could mistake for "requires nothing".
    private enum SubrulePosKind
    {
        NoActiveSubrules,
        Unrestricted,
        Restricted,
    }

    private sealed record SubrulePosResolution(SubrulePosKind Kind, HashSet<string> PartsOfSpeech);

    private static SubrulePosResolution SubruleRequiredPartsOfSpeech(XElement rule)
    {
        List<XElement> subrules = (
            rule.Element("PhonologicalSubrules")?.Elements("PhonologicalSubrule") ?? Enumerable.Empty<XElement>()
        )
            .Where(IsActiveElement)
            .ToList();
        if (subrules.Count == 0)
        {
            return new SubrulePosResolution(
                SubrulePosKind.NoActiveSubrules,
                new HashSet<string>(StringComparer.Ordinal)
            );
        }

        var union = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement subrule in subrules)
        {
            HashSet<string>? pos = RequiredPartsOfSpeech(subrule);
            if (pos is null || pos.Count == 0)
            {
                return new SubrulePosResolution(
                    SubrulePosKind.Unrestricted,
                    new HashSet<string>(StringComparer.Ordinal)
                );
            }
            union.UnionWith(pos);
        }
        return new SubrulePosResolution(SubrulePosKind.Restricted, union);
    }

    private static PosDisjointCheck CheckPhonologicalPair(XDocument grammar, OrderingItem item)
    {
        XElement? ruleA = FindElementById(grammar, item.MemberA);
        XElement? ruleB = FindElementById(grammar, item.MemberB);
        if (ruleA is null || ruleB is null)
        {
            string missing = ruleA is null ? item.MemberA : item.MemberB;
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Undetermined,
                $"'{missing}' was not found as an element with a matching id attribute"
            );
        }
        if (ruleA.Name.LocalName != "PhonologicalRule" || ruleB.Name.LocalName != "PhonologicalRule")
        {
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Undetermined,
                $"<{ruleA.Name.LocalName}>/<{ruleB.Name.LocalName}> is not modeled -- only a PhonologicalRule/PhonologicalRule "
                    + "pair has PhonologicalSubrule requiredPartsOfSpeech to compare (MetathesisRule has none)"
            );
        }

        SubrulePosResolution resA = SubruleRequiredPartsOfSpeech(ruleA);
        SubrulePosResolution resB = SubruleRequiredPartsOfSpeech(ruleB);
        if (resA.Kind == SubrulePosKind.NoActiveSubrules || resB.Kind == SubrulePosKind.NoActiveSubrules)
        {
            string missing = resA.Kind == SubrulePosKind.NoActiveSubrules ? item.MemberA : item.MemberB;
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Undetermined,
                $"'{missing}' has no active PhonologicalSubrule children"
            );
        }
        if (resA.Kind == SubrulePosKind.Unrestricted || resB.Kind == SubrulePosKind.Unrestricted)
        {
            string unrestricted = resA.Kind == SubrulePosKind.Unrestricted ? item.MemberA : item.MemberB;
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Overlaps,
                $"'{unrestricted}' has an active PhonologicalSubrule with no requiredPartsOfSpeech, so it applies regardless of part of speech"
            );
        }

        string[] shared = resA
            .PartsOfSpeech.Intersect(resB.PartsOfSpeech, StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        if (shared.Length > 0)
        {
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Overlaps,
                $"requiredPartsOfSpeech share {{{string.Join(",", shared)}}}"
            );
        }

        HashSet<string> union = resA
            .PartsOfSpeech.Union(resB.PartsOfSpeech, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        XElement? bridge = FindBridgingRule(grammar, union, resA.PartsOfSpeech, resB.PartsOfSpeech);
        if (bridge is not null)
        {
            return new PosDisjointCheck(
                item.Id,
                PosRelation.Undetermined,
                $"possible bridge: <{bridge.Name.LocalName}> '{(string?)bridge.Attribute("id")}' outputs a part of speech "
                    + $"in {{{string.Join(",", union.OrderBy(s => s, StringComparer.Ordinal))}}}"
            );
        }

        return new PosDisjointCheck(
            item.Id,
            PosRelation.Disjoint,
            $"'{item.MemberA}' PhonologicalSubrules require {{{string.Join(",", resA.PartsOfSpeech.OrderBy(s => s, StringComparer.Ordinal))}}}, "
                + $"'{item.MemberB}' PhonologicalSubrules require {{{string.Join(",", resB.PartsOfSpeech.OrderBy(s => s, StringComparer.Ordinal))}}}, "
                + "disjoint, and no modeled active MorphologicalRule/CompoundingRule can bridge the compared part-of-speech sets "
                + "(same-side MorphologicalRule preservation is excluded)"
        );
    }

    private static HashSet<string>? RequiredPartsOfSpeech(XElement rule)
    {
        string? value = (string?)rule.Attribute("requiredPartsOfSpeech");
        return string.IsNullOrEmpty(value)
            ? null
            : value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
    }

    // Single-hop over-approximation: every active MorphologicalRule/CompoundingRule in the whole document
    // (not just the owning Stratum) whose outputPartOfSpeech lands in the union is a candidate bridge,
    // regardless of whether that rule's own inputs are themselves reachable. A MorphologicalRule is
    // exempt only when its nonempty required POS set is wholly contained in the same side as its output;
    // CompoundingRule remains conservative because its input shape is different. See this file's header.
    private static XElement? FindBridgingRule(
        XDocument grammar,
        HashSet<string> union,
        HashSet<string> requiredA,
        HashSet<string> requiredB
    ) =>
        grammar
            .Descendants()
            .Where(e => e.Name.LocalName is "MorphologicalRule" or "CompoundingRule")
            .Where(IsActiveElement)
            .FirstOrDefault(e =>
            {
                string? outPos = (string?)e.Attribute("outputPartOfSpeech");
                if (string.IsNullOrEmpty(outPos) || !union.Contains(outPos))
                    return false;

                return !IsSameSideMorphologicalPreservation(e, outPos, requiredA, requiredB);
            });

    private static bool IsSameSideMorphologicalPreservation(
        XElement candidate,
        string outputPartOfSpeech,
        HashSet<string> requiredA,
        HashSet<string> requiredB
    )
    {
        if (candidate.Name.LocalName != "MorphologicalRule")
            return false;

        HashSet<string>? required = RequiredPartsOfSpeech(candidate);
        if (required is null || required.Count == 0)
            return false;

        bool outputOnSideA = requiredA.Contains(outputPartOfSpeech);
        bool outputOnSideB = requiredB.Contains(outputPartOfSpeech);
        return (outputOnSideA && required.IsSubsetOf(requiredA)) || (outputOnSideB && required.IsSubsetOf(requiredB));
    }

    private static bool IsActiveElement(XElement element) => (string?)element.Attribute("isActive") != "no";

    private static XElement? FindElementById(XDocument grammar, string id) =>
        grammar.Descendants().FirstOrDefault(e => (string?)e.Attribute("id") == id);
}
