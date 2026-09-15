#nullable enable
using System;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Builds and re-verifies a <see cref="Proof"/> of kind <see cref="Kind"/>: one member of an adjacent
/// pair can never affect the resolved rule list, regardless of which sibling it sits next to, because
/// <c>XmlLanguageLoader</c> either drops the member outright or the member itself resolves to nothing.
/// Two mechanisms, both recomputed from the current document:
/// <list type="bullet">
/// <item>the member itself carries <c>isActive="no"</c>, so <c>LoadStratum</c>'s
/// <c>MorphologicalRuleDefinitions</c>/<c>PhonologicalRuleDefinitions</c> filters or
/// <c>LoadAffixTemplate</c>'s Slot filter drop it before it is ever placed in the loaded collection an
/// IDREFS list orders;</item>
/// <item>for an AffixTemplateSlots pair, the member Slot is itself active but every id its
/// <c>morphologicalRules</c> attribute names is either inactive or undeclared (including a Slot with no
/// such attribute at all), so <c>LoadAffixTemplate</c>'s per-id <c>TryGetValue</c> against the
/// already-filtered rule dictionary silently drops every one of them and the Slot contributes no
/// morpheme wherever it sits.</item>
/// </list>
/// Re-activating a member, or adding an id that resolves to an active rule to an empty Slot, invalidates
/// the corresponding proof.
/// </summary>
public static class InactiveMemberProofs
{
    public const string Kind = "inactive-member";

    /// <summary>Builds a <see cref="Kind"/> proof for <paramref name="item"/>, or null when neither
    /// member is inactive or empty.</summary>
    public static Proof? TryBuild(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);
        InactiveMemberCheck check = Check(grammar, item);
        return check.HasInactiveMember ? new Proof(item.Id, Kind, check.Reason) : null;
    }

    /// <summary>
    /// Re-verifies <paramref name="proof"/>: true only when <paramref name="fixtureId"/>'s freshly
    /// generated adjacent pairs still contain an item with <paramref name="proof"/>'s id AND at least one
    /// current member either carries <c>isActive="no"</c> itself or (for an AffixTemplateSlots pair)
    /// resolves to no active rule. A stale id, an unresolvable member, and two contributing members all
    /// fail closed to false.
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
        return Check(grammar, item).HasInactiveMember;
    }

    private sealed record InactiveMemberCheck(string ItemId, bool HasInactiveMember, string Reason);

    private static InactiveMemberCheck Check(XDocument grammar, OrderingItem item) =>
        item.Kind == OrderingListKind.AffixTemplateSlots ? CheckSlotPair(grammar, item) : CheckIdrefPair(grammar, item);

    // StratumMorphologicalRules/StratumPhonologicalRules members are IDREFS, unique document-wide, so
    // any element carrying a matching id attribute is the declaration -- regardless of element type.
    private static InactiveMemberCheck CheckIdrefPair(XDocument grammar, OrderingItem item)
    {
        XElement? elementA = FindElementById(grammar, item.MemberA);
        XElement? elementB = FindElementById(grammar, item.MemberB);
        if (elementA is null || elementB is null)
        {
            string missing = elementA is null ? item.MemberA : item.MemberB;
            return new InactiveMemberCheck(
                item.Id,
                false,
                $"'{missing}' was not found as an element with a matching id attribute"
            );
        }

        if (!IsActiveElement(elementA))
        {
            return new InactiveMemberCheck(
                item.Id,
                true,
                $"'{item.MemberA}' (<{elementA.Name.LocalName}>) has isActive=\"no\""
            );
        }
        if (!IsActiveElement(elementB))
        {
            return new InactiveMemberCheck(
                item.Id,
                true,
                $"'{item.MemberB}' (<{elementB.Name.LocalName}>) has isActive=\"no\""
            );
        }
        return new InactiveMemberCheck(item.Id, false, "both members are active");
    }

    // AffixTemplateSlots members are a Slot's free-text Name, not a unique id, so the pair is relocated
    // positionally within its owning AffixTemplate -- the same resolution OrderingGenerator.Swap uses.
    // An active Slot whose morphologicalRules all fail to resolve is equally a no-op (EmptySlotReason).
    private static InactiveMemberCheck CheckSlotPair(XDocument grammar, OrderingItem item)
    {
        XElement[] templates = grammar.Descendants("AffixTemplate").ToArray();
        if (item.OwnerOrdinal < 0 || item.OwnerOrdinal >= templates.Length)
        {
            return new InactiveMemberCheck(
                item.Id,
                false,
                "owning AffixTemplate ordinal is out of range for the current document"
            );
        }

        var slots = templates[item.OwnerOrdinal].Elements("Slot").ToList();
        if (item.PairIndex < 0 || item.PairIndex + 1 >= slots.Count)
            return new InactiveMemberCheck(item.Id, false, "Slot pair index is out of range for the current document");

        XElement slotA = slots[item.PairIndex];
        XElement slotB = slots[item.PairIndex + 1];
        if (SlotLabel(slotA, item.PairIndex) != item.MemberA || SlotLabel(slotB, item.PairIndex + 1) != item.MemberB)
        {
            return new InactiveMemberCheck(
                item.Id,
                false,
                "the Slot pair no longer matches the document at this position"
            );
        }

        if (!IsActiveElement(slotA))
            return new InactiveMemberCheck(item.Id, true, $"Slot '{item.MemberA}' has isActive=\"no\"");
        if (!IsActiveElement(slotB))
            return new InactiveMemberCheck(item.Id, true, $"Slot '{item.MemberB}' has isActive=\"no\"");

        string? emptyA = EmptySlotReason(grammar, slotA, item.MemberA);
        if (emptyA is not null)
            return new InactiveMemberCheck(item.Id, true, emptyA);
        string? emptyB = EmptySlotReason(grammar, slotB, item.MemberB);
        if (emptyB is not null)
            return new InactiveMemberCheck(item.Id, true, emptyB);

        return new InactiveMemberCheck(
            item.Id,
            false,
            "both Slot members are active and resolve to at least one active rule"
        );
    }

    // Mirrors LoadAffixTemplate's own resolution exactly: split the Slot's morphologicalRules IDREFS
    // (absent counts as zero ids, matching an #IMPLIED-style empty list), and keep only ids that name a
    // declared AND active rule. Null return means the Slot resolves to at least one active rule, so it
    // is not empty.
    private static string? EmptySlotReason(XDocument grammar, XElement slot, string slotLabel)
    {
        string? rulesAttribute = (string?)slot.Attribute("morphologicalRules");
        string[] ruleIds = string.IsNullOrEmpty(rulesAttribute)
            ? Array.Empty<string>()
            : rulesAttribute.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (ruleIds.Length == 0)
            return $"Slot '{slotLabel}' declares no morphologicalRules and so resolves to no rule";

        bool anyActive = ruleIds.Any(ruleId =>
        {
            XElement? rule = FindElementById(grammar, ruleId);
            return rule is not null && IsActiveElement(rule);
        });
        return anyActive
            ? null
            : $"Slot '{slotLabel}''s referenced rule(s) {{{string.Join(",", ruleIds)}}} all fail to resolve to an active rule definition";
    }

    private static string SlotLabel(XElement slot, int index)
    {
        string? name = (string?)slot.Element("Name");
        return string.IsNullOrEmpty(name) ? $"Slot#{index}" : name;
    }

    private static XElement? FindElementById(XDocument grammar, string id) =>
        grammar.Descendants().FirstOrDefault(e => (string?)e.Attribute("id") == id);

    private static bool IsActiveElement(XElement element) => (string?)element.Attribute("isActive") != "no";
}
