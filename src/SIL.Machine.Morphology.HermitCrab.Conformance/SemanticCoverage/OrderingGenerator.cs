#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Which ordered list an <see cref="OrderedList"/>/<see cref="OrderingItem"/> came from.</summary>
public enum OrderingListKind
{
    /// <summary>A Stratum's <c>phonologicalRules</c> IDREFS attribute.</summary>
    StratumPhonologicalRules,

    /// <summary>A Stratum's <c>morphologicalRules</c> IDREFS attribute.</summary>
    StratumMorphologicalRules,

    /// <summary>An AffixTemplate's ordered <c>Slot</c> element children.</summary>
    AffixTemplateSlots,

    /// <summary>
    /// Not produced by <see cref="OrderingGenerator.EnumerateAdjacentPairs"/> -- a synthetic tag used only to
    /// route a <see cref="StratumInteractionPair"/> through <see cref="OrderingGenerator.CheckDisjointDomains"/>
    /// when it is not a PhonologicalRule/PhonologicalRule pair, so the reported reason names what it actually is.
    /// </summary>
    StratumPair,
}

/// <summary>Which pipeline stage a <see cref="StratumUnit"/> belongs to, per
/// <see cref="OrderingGenerator.EnumerateStratumPairs"/>.</summary>
public enum StratumUnitKind
{
    /// <summary>A Stratum's <c>morphologicalRules</c> IDREFS member.</summary>
    MorphologicalRule,

    /// <summary>
    /// A whole AffixTemplate -- never one of its Slot rules individually. Slot order within one template is
    /// already pinned by <see cref="OrderingListKind.AffixTemplateSlots"/>; the template as a unit is what
    /// competes with a Stratum's free morphological rules and phonological rules for pipeline position.
    /// </summary>
    AffixTemplate,

    /// <summary>A Stratum's <c>phonologicalRules</c> IDREFS member.</summary>
    PhonologicalRule,
}

/// <summary>One rule-bearing unit a <see cref="StratumInteractionPair"/> relates -- see
/// <see cref="StratumUnitKind"/>.</summary>
public sealed record StratumUnit(StratumUnitKind Kind, string Label);

/// <summary>
/// Why a <see cref="StratumInteractionPair"/> is pipeline-permitted, per the engine's synthesis order
/// (<c>SynthesisStratumRule.Apply</c>): morphology (free rules and templates, mutually recursive) runs
/// before phonology within one Stratum, and one Stratum's phonological output is the next Stratum's
/// morphological input.
/// </summary>
public enum StratumPairKind
{
    /// <summary>Both units are in the same pipeline stage (both morphology, or both phonology) of one
    /// Stratum.</summary>
    SameStage,

    /// <summary>A morphology-stage unit paired with a phonology-stage unit of the SAME Stratum, in
    /// that order only.</summary>
    CrossStage,

    /// <summary>A phonology-stage unit of one Stratum paired with a morphology-stage unit of the NEXT
    /// Stratum.</summary>
    CrossStratum,
}

/// <summary>
/// One pipeline-permitted ordered pair from <see cref="OrderingGenerator.EnumerateStratumPairs"/> -- the
/// denominator for rule-interaction coverage, complementing <see cref="OrderingItem"/>'s adjacent-only pairs
/// with every non-adjacent, cross-list, cross-stage, and cross-stratum pair the engine's pipeline order
/// actually permits.
/// </summary>
public sealed record StratumInteractionPair(
    string Id,
    string FixtureId,
    StratumPairKind Kind,
    string StratumALabel,
    int StratumAOrdinal,
    StratumUnit UnitA,
    string StratumBLabel,
    int StratumBOrdinal,
    StratumUnit UnitB
);

/// <summary>
/// One ordered declaration found in a fixture's grammar, with >= 2 members -- the unit
/// <see cref="OrderingGenerator.EnumerateAdjacentPairs"/> turns into adjacent-transposition items.
/// </summary>
public sealed record OrderedList(
    string FixtureId,
    OrderingListKind Kind,
    string OwnerLabel,
    int OwnerOrdinal,
    IReadOnlyList<string> Members
);

/// <summary>
/// One adjacent pair from an <see cref="OrderedList"/>. Adjacent transpositions generate the symmetric
/// group, so pinning every adjacent swap pins the whole declared order -- this is deliberately n-1
/// items per list, never the C(n,2) pairs or n! permutations.
/// </summary>
public sealed record OrderingItem(
    string Id,
    string FixtureId,
    OrderingListKind Kind,
    string OwnerLabel,
    int OwnerOrdinal,
    int PairIndex,
    string MemberA,
    string MemberB
);

/// <summary>The document produced by transposing one <see cref="OrderingItem"/>'s adjacent pair.</summary>
public sealed record OrderingSwap(string ItemId, string Detail, XDocument Mutated);

/// <summary>
/// Whether an earlier rule can possibly affect anything the later rule is sensitive to, per
/// <see cref="OrderingGenerator.CheckDisjointDomains"/>. Deliberately three-valued: an adjacent pair
/// whose interaction cannot be resolved must never be reported as <see cref="Disjoint"/>, because that
/// would license the claim "these two provably do not interact" on nothing but an inability to look.
/// </summary>
public enum DomainRelation
{
    /// <summary>
    /// The earlier rule's EFFECT (its output segments plus its own input segments, since altering or
    /// consuming a segment can matter as much as producing one) and the later rule's SENSITIVE set (its
    /// input segments plus every segment reachable from its Environment/LeftEnvironment/RightEnvironment
    /// templates) do not intersect.
    /// </summary>
    Disjoint,

    /// <summary>They share at least one segment, so the pair can genuinely interact -- including
    /// feeding and bleeding via Environment.</summary>
    Overlaps,

    /// <summary>Something in the pair could not be resolved to a segment set. Never treated as Disjoint.</summary>
    Undetermined,
}

/// <summary>The disjoint-domains verdict for one <see cref="OrderingItem"/>, with the reasoning that
/// produced it.</summary>
public sealed record DisjointDomainsCheck(string ItemId, DomainRelation Relation, string Reason);

/// <summary>
/// Generates the Ordering branch of the coverage inventory: every adjacent pair in a Stratum's
/// <c>phonologicalRules</c>/<c>morphologicalRules</c> IDREFS lists and every AffixTemplate's ordered
/// Slot children, plus the two operations a counterfactual sweep needs against them -- producing the
/// swapped grammar (<see cref="Swap"/>) and, where the swap is expected to show no delta, a static
/// independence check (<see cref="CheckDisjointDomains"/>) that fails closed to
/// <see cref="DomainRelation.Undetermined"/> rather than ever guessing <see cref="DomainRelation.Disjoint"/>.
/// </summary>
public static class OrderingGenerator
{
    public const string IdPrefix = "ordering:";

    /// <summary>Every ordered list in <paramref name="grammar"/> with two or more members.</summary>
    public static IReadOnlyList<OrderedList> EnumerateOrderedLists(XDocument grammar, string fixtureId)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(fixtureId);

        var lists = new List<OrderedList>();

        XElement[] strata = grammar.Descendants("Stratum").ToArray();
        for (int i = 0; i < strata.Length; i++)
        {
            string ownerLabel = OwnerLabel(strata[i], "Stratum", i);
            AddIdrefsList(lists, fixtureId, OrderingListKind.StratumPhonologicalRules, ownerLabel, i, strata[i].Attribute("phonologicalRules"));
            AddIdrefsList(lists, fixtureId, OrderingListKind.StratumMorphologicalRules, ownerLabel, i, strata[i].Attribute("morphologicalRules"));
        }

        XElement[] templates = grammar.Descendants("AffixTemplate").ToArray();
        for (int i = 0; i < templates.Length; i++)
        {
            string ownerLabel = OwnerLabel(templates[i], "AffixTemplate", i);
            List<XElement> slots = templates[i].Elements("Slot").ToList();
            List<string> members = slots.Select(SlotLabel).ToList();
            if (members.Count >= 2)
                lists.Add(new OrderedList(fixtureId, OrderingListKind.AffixTemplateSlots, ownerLabel, i, members));
        }

        return lists;
    }

    /// <summary>Every adjacent pair across every list <see cref="EnumerateOrderedLists"/> finds.</summary>
    public static IReadOnlyList<OrderingItem> EnumerateAdjacentPairs(XDocument grammar, string fixtureId)
    {
        var items = new List<OrderingItem>();
        foreach (OrderedList list in EnumerateOrderedLists(grammar, fixtureId))
        {
            for (int i = 0; i < list.Members.Count - 1; i++)
            {
                items.Add(
                    new OrderingItem(
                        BuildId(list, i),
                        list.FixtureId,
                        list.Kind,
                        list.OwnerLabel,
                        list.OwnerOrdinal,
                        i,
                        list.Members[i],
                        list.Members[i + 1]
                    )
                );
            }
        }
        return items;
    }

    public const string StratumPairIdPrefix = "stratum-pair:";

    /// <summary>
    /// Every pipeline-permitted ordered pair of rule-bearing units, within a Stratum or between two
    /// adjacent Strata -- the complement of <see cref="EnumerateAdjacentPairs"/>'s adjacent-only,
    /// single-list pairs. An AffixTemplate is one unit (see <see cref="StratumUnitKind.AffixTemplate"/>);
    /// self-pairs are included, because <c>multipleApplicationOrder</c> defaults every PhonologicalRule to
    /// iterative (self-feeding) application and morphological rules can recur across an Unordered
    /// stratum's mrules/template recursion -- excluding them would silently drop that default behavior.
    ///
    /// Within one Stratum, morphological rules and AffixTemplates are mutually recursive
    /// (<c>SynthesisStratumRule.ApplyMorphologicalRules</c> calls <c>ApplyTemplates</c> and vice versa), so
    /// every ordered pair between them, in both directions, is pipeline-permitted (<see
    /// cref="StratumPairKind.SameStage"/>). Phonological rules are always run through an unconditional
    /// <c>LinearRuleCascade</c> (<c>SynthesisStratumRule</c> never branches on <c>phonologicalRuleOrder</c>),
    /// so a same-stage phonology pair is permitted only forward or self, per the declared
    /// <c>phonologicalRules</c> order -- a later rule can never feed an earlier one within one pass.
    /// Morphology precedes phonology within one Stratum's synthesis pass and never the reverse, so
    /// cross-stage pairs run one way only. Across Strata, one Stratum's phonological output is the next
    /// Stratum's morphological input (<c>Language.CompileSynthesisRule</c> cascades Strata in declared
    /// order); only that adjacent, forward relationship is modeled.
    /// </summary>
    public static IReadOnlyList<StratumInteractionPair> EnumerateStratumPairs(XDocument grammar, string fixtureId)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(fixtureId);

        XElement[] strata = grammar.Descendants("Stratum").ToArray();
        XElement[] allTemplates = grammar.Descendants("AffixTemplate").ToArray();

        var stages = new (string Label, List<StratumUnit> Morphology, List<StratumUnit> Phonology)[strata.Length];
        for (int i = 0; i < strata.Length; i++)
        {
            string label = OwnerLabel(strata[i], "Stratum", i);
            var morphology = new List<StratumUnit>();
            foreach (string id in SplitIdrefs(strata[i].Attribute("morphologicalRules")?.Value ?? ""))
                morphology.Add(new StratumUnit(StratumUnitKind.MorphologicalRule, id));
            for (int t = 0; t < allTemplates.Length; t++)
            {
                if (allTemplates[t].Ancestors("Stratum").FirstOrDefault() == strata[i])
                    morphology.Add(new StratumUnit(StratumUnitKind.AffixTemplate, OwnerLabel(allTemplates[t], "AffixTemplate", t)));
            }
            var phonology = SplitIdrefs(strata[i].Attribute("phonologicalRules")?.Value ?? "")
                .Select(id => new StratumUnit(StratumUnitKind.PhonologicalRule, id))
                .ToList();
            stages[i] = (label, morphology, phonology);
        }

        var pairs = new List<StratumInteractionPair>();
        for (int i = 0; i < strata.Length; i++)
        {
            (string label, List<StratumUnit> morphology, List<StratumUnit> phonology) = stages[i];

            foreach (StratumUnit a in morphology)
            {
                foreach (StratumUnit b in morphology)
                    pairs.Add(BuildStratumPair(fixtureId, StratumPairKind.SameStage, label, i, a, label, i, b));
            }

            for (int a = 0; a < phonology.Count; a++)
            {
                for (int b = a; b < phonology.Count; b++)
                    pairs.Add(BuildStratumPair(fixtureId, StratumPairKind.SameStage, label, i, phonology[a], label, i, phonology[b]));
            }

            foreach (StratumUnit m in morphology)
            {
                foreach (StratumUnit p in phonology)
                    pairs.Add(BuildStratumPair(fixtureId, StratumPairKind.CrossStage, label, i, m, label, i, p));
            }

            if (i + 1 < strata.Length)
            {
                (string nextLabel, List<StratumUnit> nextMorphology, _) = stages[i + 1];
                foreach (StratumUnit p in phonology)
                {
                    foreach (StratumUnit m in nextMorphology)
                        pairs.Add(BuildStratumPair(fixtureId, StratumPairKind.CrossStratum, label, i, p, nextLabel, i + 1, m));
                }
            }
        }

        return pairs;
    }

    /// <summary>
    /// Classifies a <see cref="StratumInteractionPair"/> by delegating to <see cref="CheckDisjointDomains"/> --
    /// only a PhonologicalRule/PhonologicalRule pair can ever resolve to <see cref="DomainRelation.Disjoint"/>
    /// or <see cref="DomainRelation.Overlaps"/>; every other unit-kind combination reports
    /// <see cref="DomainRelation.Undetermined"/> by construction, matching that check's own stated scope.
    /// </summary>
    public static DisjointDomainsCheck CheckStratumPairDisjointDomains(XDocument grammar, StratumInteractionPair pair)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(pair);

        bool bothPhonological =
            pair.UnitA.Kind == StratumUnitKind.PhonologicalRule && pair.UnitB.Kind == StratumUnitKind.PhonologicalRule;
        var syntheticItem = new OrderingItem(
            pair.Id,
            pair.FixtureId,
            bothPhonological ? OrderingListKind.StratumPhonologicalRules : OrderingListKind.StratumPair,
            pair.StratumALabel,
            pair.StratumAOrdinal,
            0,
            pair.UnitA.Label,
            pair.UnitB.Label
        );
        return CheckDisjointDomains(grammar, syntheticItem);
    }

    private static StratumInteractionPair BuildStratumPair(
        string fixtureId,
        StratumPairKind kind,
        string stratumALabel,
        int stratumAOrdinal,
        StratumUnit unitA,
        string stratumBLabel,
        int stratumBOrdinal,
        StratumUnit unitB
    )
    {
        string id =
            $"{StratumPairIdPrefix}{fixtureId}/{stratumAOrdinal}:{StratumUnitTag(unitA.Kind)}:{Encode(unitA.Label)}"
                + $"~{stratumBOrdinal}:{StratumUnitTag(unitB.Kind)}:{Encode(unitB.Label)}";
        return new StratumInteractionPair(id, fixtureId, kind, stratumALabel, stratumAOrdinal, unitA, stratumBLabel, stratumBOrdinal, unitB);
    }

    private static string StratumUnitTag(StratumUnitKind kind) =>
        kind switch
        {
            StratumUnitKind.MorphologicalRule => "mrule",
            StratumUnitKind.AffixTemplate => "template",
            StratumUnitKind.PhonologicalRule => "prule",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    /// <summary>
    /// The same grammar with exactly <paramref name="item"/>'s adjacent pair transposed, so the result
    /// can be saved and run through <c>--evaluate-mutant</c>. Never mutates <paramref name="grammar"/>
    /// -- a copy is made first, matching <see cref="GrammarMutator"/>'s established pattern. Returns
    /// null when <paramref name="item"/> no longer matches the document (wrong owner count, member
    /// order changed underneath it), rather than silently swapping the wrong pair.
    /// </summary>
    public static OrderingSwap? Swap(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);

        var copy = new XDocument(grammar);
        return item.Kind switch
        {
            OrderingListKind.StratumPhonologicalRules => SwapIdrefsAttribute(copy, item, "phonologicalRules"),
            OrderingListKind.StratumMorphologicalRules => SwapIdrefsAttribute(copy, item, "morphologicalRules"),
            OrderingListKind.AffixTemplateSlots => SwapSlots(copy, item),
            _ => null,
        };
    }

    /// <summary>
    /// Decides whether <paramref name="item"/>'s earlier member can possibly affect anything its later
    /// member is sensitive to, recomputed from <paramref name="grammar"/>. Only Stratum
    /// <c>phonologicalRules</c> pairs between two <c>PhonologicalRule</c> elements are ever resolved to
    /// <see cref="DomainRelation.Disjoint"/> or <see cref="DomainRelation.Overlaps"/> -- everything else
    /// (MetathesisRule, morphological-rule pairs, Slot pairs, or any construct this check does not
    /// model) is reported as <see cref="DomainRelation.Undetermined"/> with the specific reason, never
    /// guessed.
    ///
    /// The earlier rule's EFFECT set is its output segments (what it produces) unioned with its own input
    /// segments (what it consumes or alters) -- removing or changing a segment can destroy an environment
    /// the later rule needed (bleeding) just as much as producing one can create it (feeding). The later
    /// rule's SENSITIVE set is its input segments unioned with every segment reachable from its
    /// Environment/LeftEnvironment/RightEnvironment templates, per <c>PhonologicalSubrule</c>, since a
    /// rule that only fires in a given environment is affected by anything that can create or destroy that
    /// environment. <see cref="DomainRelation.Disjoint"/> holds only when EFFECT and SENSITIVE do not
    /// intersect.
    /// </summary>
    public static DisjointDomainsCheck CheckDisjointDomains(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);

        if (item.Kind != OrderingListKind.StratumPhonologicalRules)
        {
            return new DisjointDomainsCheck(
                item.Id,
                DomainRelation.Undetermined,
                $"{item.Kind} pairs are not modeled by this check -- only Stratum phonologicalRules pairs "
                    + "resolve an effect-segment-set against a sensitivity-segment-set"
            );
        }

        XElement? ruleA = FindPhonologicalOrMetathesisRule(grammar, item.MemberA);
        XElement? ruleB = FindPhonologicalOrMetathesisRule(grammar, item.MemberB);
        if (ruleA is null || ruleB is null)
        {
            return new DisjointDomainsCheck(
                item.Id,
                DomainRelation.Undetermined,
                $"'{item.MemberA}' or '{item.MemberB}' was not found as a PhonologicalRule/MetathesisRule in the document"
            );
        }
        if (ruleA.Name.LocalName != "PhonologicalRule" || ruleB.Name.LocalName != "PhonologicalRule")
        {
            return new DisjointDomainsCheck(
                item.Id,
                DomainRelation.Undetermined,
                "MetathesisRule's StructuralDescription is not modeled by this check"
            );
        }

        var cache = new Dictionary<string, SegmentResolution>(StringComparer.Ordinal);
        SegmentResolution effect = ResolveRuleEffectSegments(ruleA, grammar, cache);
        if (!effect.IsResolved)
        {
            return new DisjointDomainsCheck(
                item.Id,
                DomainRelation.Undetermined,
                $"could not resolve '{item.MemberA}''s effect segments: {effect.Reason}"
            );
        }

        SegmentResolution sensitive = ResolveRuleSensitiveSegments(ruleB, grammar, cache);
        if (!sensitive.IsResolved)
        {
            return new DisjointDomainsCheck(
                item.Id,
                DomainRelation.Undetermined,
                $"could not resolve '{item.MemberB}''s sensitivity segments: {sensitive.Reason}"
            );
        }

        string[] shared = effect.Segments.Intersect(sensitive.Segments, StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        return shared.Length == 0
            ? new DisjointDomainsCheck(
                item.Id,
                DomainRelation.Disjoint,
                $"'{item.MemberA}' effect (output+input) {{{string.Join(",", effect.Segments.OrderBy(s => s, StringComparer.Ordinal))}}} and "
                    + $"'{item.MemberB}' sensitivity (input+environment) {{{string.Join(",", sensitive.Segments.OrderBy(s => s, StringComparer.Ordinal))}}} do not intersect"
            )
            : new DisjointDomainsCheck(item.Id, DomainRelation.Overlaps, $"shared segment(s): {string.Join(",", shared)}");
    }

    private static void AddIdrefsList(
        List<OrderedList> lists,
        string fixtureId,
        OrderingListKind kind,
        string ownerLabel,
        int ownerOrdinal,
        XAttribute? attribute
    )
    {
        if (attribute is null)
            return;
        string[] members = SplitIdrefs(attribute.Value);
        if (members.Length >= 2)
            lists.Add(new OrderedList(fixtureId, kind, ownerLabel, ownerOrdinal, members));
    }

    private static string[] SplitIdrefs(string value) => value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static string OwnerLabel(XElement element, string tag, int ordinal)
    {
        string? name = (string?)element.Element("Name");
        return string.IsNullOrEmpty(name) ? $"{tag}#{ordinal}" : name;
    }

    private static string SlotLabel(XElement slot, int index)
    {
        string? name = (string?)slot.Element("Name");
        return string.IsNullOrEmpty(name) ? $"Slot#{index}" : name;
    }

    private static string AttributeSlug(OrderingListKind kind) =>
        kind switch
        {
            OrderingListKind.StratumPhonologicalRules => "phonologicalRules",
            OrderingListKind.StratumMorphologicalRules => "morphologicalRules",
            OrderingListKind.AffixTemplateSlots => "slots",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    // fixtureId is kept raw (it is already a plain "category/name" path this repo controls), so only
    // the member labels -- which a Slot's free-text Name can make arbitrary -- are percent-encoded.
    // Deliberately omits OwnerOrdinal: within one fixture, IDREFS tokens are unique document-wide
    // (XML ID uniqueness), so a member pair alone already disambiguates every Stratum list. The one
    // gap that leaves is two AffixTemplates in the same fixture reusing an identical Slot Name pair,
    // which this scheme cannot distinguish; see this file's caller-facing report for that limitation.
    private static string BuildId(OrderedList list, int pairIndex) =>
        $"{IdPrefix}{list.FixtureId}/{AttributeSlug(list.Kind)}/{Encode(list.Members[pairIndex])}~{Encode(list.Members[pairIndex + 1])}";

    private static string Encode(string value) => CanonicalIdCodec.Encode(value);

    private static OrderingSwap? SwapIdrefsAttribute(XDocument copy, OrderingItem item, string attributeName)
    {
        XElement[] strata = copy.Descendants("Stratum").ToArray();
        if (item.OwnerOrdinal < 0 || item.OwnerOrdinal >= strata.Length)
            return null;
        XElement owner = strata[item.OwnerOrdinal];
        XAttribute? attribute = owner.Attribute(attributeName);
        if (attribute is null)
            return null;

        string[] tokens = SplitIdrefs(attribute.Value);
        if (item.PairIndex < 0 || item.PairIndex + 1 >= tokens.Length)
            return null;
        if (tokens[item.PairIndex] != item.MemberA || tokens[item.PairIndex + 1] != item.MemberB)
            return null;

        (tokens[item.PairIndex], tokens[item.PairIndex + 1]) = (tokens[item.PairIndex + 1], tokens[item.PairIndex]);
        attribute.Value = string.Join(" ", tokens);
        return new OrderingSwap(
            item.Id,
            $"swapped adjacent {attributeName} entries \"{item.MemberA}\" and \"{item.MemberB}\" on "
                + $"Stratum#{item.OwnerOrdinal} (\"{item.OwnerLabel}\")",
            copy
        );
    }

    private static OrderingSwap? SwapSlots(XDocument copy, OrderingItem item)
    {
        XElement[] templates = copy.Descendants("AffixTemplate").ToArray();
        if (item.OwnerOrdinal < 0 || item.OwnerOrdinal >= templates.Length)
            return null;
        XElement owner = templates[item.OwnerOrdinal];
        List<XElement> slots = owner.Elements("Slot").ToList();
        if (item.PairIndex < 0 || item.PairIndex + 1 >= slots.Count)
            return null;

        XElement first = slots[item.PairIndex];
        XElement second = slots[item.PairIndex + 1];
        if (SlotLabel(first, item.PairIndex) != item.MemberA || SlotLabel(second, item.PairIndex + 1) != item.MemberB)
            return null;

        // Swap the two elements' positions in place via a placeholder, rather than detaching every
        // Slot and rebuilding the list, so no other slot's position is disturbed.
        var placeholder = new XElement("Placeholder");
        first.ReplaceWith(placeholder);
        second.ReplaceWith(first);
        placeholder.ReplaceWith(second);

        return new OrderingSwap(
            item.Id,
            $"swapped adjacent Slot elements \"{item.MemberA}\" and \"{item.MemberB}\" on "
                + $"AffixTemplate#{item.OwnerOrdinal} (\"{item.OwnerLabel}\")",
            copy
        );
    }

    private static XElement? FindPhonologicalOrMetathesisRule(XDocument grammar, string id) =>
        grammar
            .Descendants()
            .FirstOrDefault(e =>
                (e.Name.LocalName == "PhonologicalRule" || e.Name.LocalName == "MetathesisRule")
                && (string?)e.Attribute("id") == id
            );

    /// <summary>
    /// A resolved segment set, or a failure to resolve one -- <see cref="IsResolved"/> false means
    /// "cannot look", which callers must never treat as an empty (and therefore trivially disjoint)
    /// set.
    /// </summary>
    private readonly record struct SegmentResolution(bool IsResolved, IReadOnlySet<string> Segments, string Reason)
    {
        public static SegmentResolution Ok(IEnumerable<string> segments) => new(true, segments.ToHashSet(StringComparer.Ordinal), "");

        public static SegmentResolution No(string reason) => new(false, ImmutableHashSet<string>.Empty, reason);
    }

    private static SegmentResolution Union(SegmentResolution a, SegmentResolution b)
    {
        if (!a.IsResolved)
            return a;
        if (!b.IsResolved)
            return b;
        return SegmentResolution.Ok(a.Segments.Concat(b.Segments));
    }

    // A rule's live output is the union across every PhonologicalSubrule's PhoneticOutput: any one of
    // them may fire, so all are possible outputs. isActive="no" subrules are included too --
    // over-approximating a rule's output can only turn a real Disjoint into a false Undetermined,
    // never the reverse, which is the safe direction for a check whose only forbidden mistake is a
    // false Disjoint.
    private static SegmentResolution ResolveRuleOutputSegments(XElement rule, XDocument grammar, Dictionary<string, SegmentResolution> cache)
    {
        List<XElement> subrules = rule.Element("PhonologicalSubrules")?.Elements("PhonologicalSubrule").ToList() ?? new List<XElement>();
        if (subrules.Count == 0)
            return SegmentResolution.No("no PhonologicalSubrule children found");

        SegmentResolution acc = SegmentResolution.Ok(Array.Empty<string>());
        foreach (XElement subrule in subrules)
        {
            XElement? sequence = subrule.Element("PhoneticOutput")?.Element("PhoneticSequence");
            acc = Union(acc, ResolveSequenceSegments(sequence, grammar, cache));
            if (!acc.IsResolved)
                return acc;
        }
        return acc;
    }

    // A rule's PhoneticInput is shared across every one of its subrules (the DTD gives PhonologicalRule
    // exactly one PhoneticInput), so this is a single resolution, not a union.
    private static SegmentResolution ResolveRuleInputSegments(XElement rule, XDocument grammar, Dictionary<string, SegmentResolution> cache) =>
        ResolveSequenceSegments(rule.Element("PhoneticInput")?.Element("PhoneticSequence"), grammar, cache);

    // What an earlier rule can change: what it produces AND what it consumes/alters. Consuming or
    // altering a segment can destroy an environment a later rule needs (bleeding), so input belongs in
    // EFFECT exactly as much as output does.
    private static SegmentResolution ResolveRuleEffectSegments(XElement rule, XDocument grammar, Dictionary<string, SegmentResolution> cache)
    {
        SegmentResolution output = ResolveRuleOutputSegments(rule, grammar, cache);
        return Union(output, ResolveRuleInputSegments(rule, grammar, cache));
    }

    // What a later rule can be affected by: what it matches AND every segment its Environment templates
    // are conditioned on. A rule that only fires in some environment is sensitive to whatever can create
    // or destroy that environment, not only to its own PhoneticInput.
    private static SegmentResolution ResolveRuleSensitiveSegments(XElement rule, XDocument grammar, Dictionary<string, SegmentResolution> cache)
    {
        SegmentResolution input = ResolveRuleInputSegments(rule, grammar, cache);
        return Union(input, ResolveRuleEnvironmentSegments(rule, grammar, cache));
    }

    // Union across every PhonologicalSubrule's Environment, mirroring ResolveRuleOutputSegments: any one
    // subrule's environment can be the one that fires, so all are possible sensitivities. isActive="no"
    // subrules are included too, for the same over-approximation reason output does.
    private static SegmentResolution ResolveRuleEnvironmentSegments(XElement rule, XDocument grammar, Dictionary<string, SegmentResolution> cache)
    {
        List<XElement> subrules = rule.Element("PhonologicalSubrules")?.Elements("PhonologicalSubrule").ToList() ?? new List<XElement>();
        if (subrules.Count == 0)
            return SegmentResolution.No("no PhonologicalSubrule children found");

        SegmentResolution acc = SegmentResolution.Ok(Array.Empty<string>());
        foreach (XElement subrule in subrules)
        {
            acc = Union(acc, ResolveSubruleEnvironmentSegments(subrule, grammar, cache));
            if (!acc.IsResolved)
                return acc;
        }
        return acc;
    }

    // A subrule's Environment (LeftEnvironment?, RightEnvironment?) is optional per the DTD; absent
    // means this subrule fires unconditionally, i.e. contributes no extra environment segments.
    private static SegmentResolution ResolveSubruleEnvironmentSegments(XElement subrule, XDocument grammar, Dictionary<string, SegmentResolution> cache)
    {
        XElement? environment = subrule.Element("Environment");
        if (environment is null)
            return SegmentResolution.Ok(Array.Empty<string>());

        SegmentResolution left = ResolveEnvironmentSideSegments(environment.Element("LeftEnvironment"), grammar, cache);
        if (!left.IsResolved)
            return left;
        return Union(left, ResolveEnvironmentSideSegments(environment.Element("RightEnvironment"), grammar, cache));
    }

    private static SegmentResolution ResolveEnvironmentSideSegments(XElement? side, XDocument grammar, Dictionary<string, SegmentResolution> cache) =>
        side is null
            ? SegmentResolution.Ok(Array.Empty<string>())
            : ResolveSequenceSegments(side.Element("PhoneticTemplate")?.Element("PhoneticSequence"), grammar, cache);

    private static SegmentResolution ResolveSequenceSegments(XElement? sequence, XDocument grammar, Dictionary<string, SegmentResolution> cache)
    {
        // PhoneticInput/PhoneticOutput's PhoneticSequence child is itself optional (an epenthesis rule
        // matches/produces no pre-existing segment), and that is a fully resolved empty set, not a
        // failure to resolve.
        if (sequence is null)
            return SegmentResolution.Ok(Array.Empty<string>());

        SegmentResolution acc = SegmentResolution.Ok(Array.Empty<string>());
        foreach (XElement child in sequence.Elements())
        {
            acc = Union(acc, ResolveElementSegments(child, grammar, cache));
            if (!acc.IsResolved)
                return acc;
        }
        return acc;
    }

    private static SegmentResolution ResolveElementSegments(XElement element, XDocument grammar, Dictionary<string, SegmentResolution> cache)
    {
        switch (element.Name.LocalName)
        {
            case "Segment":
            {
                string? segmentId = (string?)element.Attribute("segment");
                return segmentId is null
                    ? SegmentResolution.No("<Segment> has no segment attribute")
                    : SegmentResolution.Ok(new[] { segmentId });
            }
            case "SimpleContext":
            {
                // AlphaVariables (if present) further narrows which polarity applies; ignoring it and
                // resolving the base natural class is an over-approximation, which is the safe
                // direction for this check.
                string? naturalClassId = (string?)element.Attribute("naturalClass");
                return naturalClassId is null
                    ? SegmentResolution.No("<SimpleContext> has no naturalClass attribute")
                    : ResolveNaturalClass(naturalClassId, grammar, cache);
            }
            case "OptionalSegmentSequence":
            {
                // Optionality means these segments may or may not appear; including them (rather than
                // excluding) is the same safe over-approximation as above.
                SegmentResolution acc = SegmentResolution.Ok(Array.Empty<string>());
                foreach (XElement child in element.Elements())
                {
                    acc = Union(acc, ResolveElementSegments(child, grammar, cache));
                    if (!acc.IsResolved)
                        return acc;
                }
                return acc;
            }
            case "BoundaryMarker":
                // A morpheme/word boundary marker, never a phonemic segment.
                return SegmentResolution.Ok(Array.Empty<string>());
            default:
                // In particular <Segments> (a raw PhoneticShape string) would need a
                // CharacterDefinitionTable tokenizer this check does not implement.
                return SegmentResolution.No($"<{element.Name.LocalName}> is not a modeled phonetic-sequence construct");
        }
    }

    private static SegmentResolution ResolveNaturalClass(string naturalClassId, XDocument grammar, Dictionary<string, SegmentResolution> cache)
    {
        if (cache.TryGetValue(naturalClassId, out SegmentResolution cached))
            return cached;

        XElement? segmentClass = grammar.Descendants("SegmentNaturalClass").FirstOrDefault(e => (string?)e.Attribute("id") == naturalClassId);
        SegmentResolution result;
        if (segmentClass is not null)
        {
            result = ResolveSegmentNaturalClass(segmentClass);
        }
        else
        {
            XElement? featureClass = grammar.Descendants("FeatureNaturalClass").FirstOrDefault(e => (string?)e.Attribute("id") == naturalClassId);
            result =
                featureClass is not null
                    ? ResolveFeatureNaturalClass(featureClass, grammar)
                    : SegmentResolution.No($"natural class '{naturalClassId}' is not declared in this document");
        }

        cache[naturalClassId] = result;
        return result;
    }

    private static SegmentResolution ResolveSegmentNaturalClass(XElement segmentClass)
    {
        var ids = new List<string>();
        foreach (XElement segment in segmentClass.Elements("Segment"))
        {
            string? segmentId = (string?)segment.Attribute("segment");
            if (segmentId is null)
            {
                return SegmentResolution.No(
                    $"SegmentNaturalClass '{(string?)segmentClass.Attribute("id")}' has a <Segment> with no segment attribute"
                );
            }
            ids.Add(segmentId);
        }
        return SegmentResolution.Ok(ids);
    }

    // Nested FeatureValue (the ComplexFeature shape) is refused rather than partially matched, and a
    // SegmentDefinition that does not declare a value for a constrained feature is Undetermined rather
    // than treated as a non-match: assuming a default one way or the other here would be guessing at
    // engine semantics this check does not verify.
    private static SegmentResolution ResolveFeatureNaturalClass(XElement featureClass, XDocument grammar)
    {
        string? classId = (string?)featureClass.Attribute("id");
        var constraints = new List<(string Feature, HashSet<string> Symbols)>();
        foreach (XElement featureValue in featureClass.Elements("FeatureValue"))
        {
            if ((string?)featureValue.Attribute("isActive") == "no")
                continue;
            if (featureValue.Elements("FeatureValue").Any())
                return SegmentResolution.No($"FeatureNaturalClass '{classId}' has a nested FeatureValue (complex-feature matching is not modeled)");

            string? feature = (string?)featureValue.Attribute("feature");
            string? symbolValues = (string?)featureValue.Attribute("symbolValues");
            if (feature is null || symbolValues is null)
                return SegmentResolution.No($"FeatureNaturalClass '{classId}' has a FeatureValue missing feature or symbolValues");
            constraints.Add((feature, SplitIdrefs(symbolValues).ToHashSet(StringComparer.Ordinal)));
        }

        var members = new List<string>();
        foreach (XElement segmentDef in grammar.Descendants("SegmentDefinition"))
        {
            if ((string?)segmentDef.Attribute("isActive") == "no")
                continue;
            string? segmentId = (string?)segmentDef.Attribute("id");
            if (segmentId is null)
                continue;

            bool? matches = MatchesEveryConstraint(segmentDef, constraints);
            if (matches is null)
                return SegmentResolution.No($"SegmentDefinition '{segmentId}' does not declare every feature FeatureNaturalClass '{classId}' constrains");
            if (matches.Value)
                members.Add(segmentId);
        }
        return SegmentResolution.Ok(members);
    }

    /// <summary>
    /// Null means at least one constrained feature is not declared (actively) on this segment at all --
    /// undetermined, never a silent non-match.
    /// </summary>
    private static bool? MatchesEveryConstraint(XElement segmentDef, List<(string Feature, HashSet<string> Symbols)> constraints)
    {
        foreach ((string feature, HashSet<string> symbols) in constraints)
        {
            XElement? declared = segmentDef
                .Elements("FeatureValue")
                .FirstOrDefault(fv => (string?)fv.Attribute("isActive") != "no" && (string?)fv.Attribute("feature") == feature);
            if (declared is null)
                return null;

            string? declaredSymbols = (string?)declared.Attribute("symbolValues");
            HashSet<string> declaredSet =
                declaredSymbols is null
                    ? new HashSet<string>(StringComparer.Ordinal)
                    : SplitIdrefs(declaredSymbols).ToHashSet(StringComparer.Ordinal);
            if (!declaredSet.Overlaps(symbols))
                return false;
        }
        return true;
    }
}
