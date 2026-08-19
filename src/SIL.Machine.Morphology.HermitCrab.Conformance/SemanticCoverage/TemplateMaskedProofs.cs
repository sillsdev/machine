#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Builds and re-verifies a <see cref="Proof"/> of kind <see cref="Kind"/>: an AffixTemplate Slot pair
/// whose owning Stratum is <c>morphologicalRuleOrder="unordered"</c> AND every rule referenced by every
/// Slot of that template (not only the pair being swapped) is also a member of the Stratum's own
/// <c>morphologicalRules</c> list. <c>SynthesisStratumRule.Apply</c> unions
/// <c>ApplyMorphologicalRules(input).Concat(ApplyTemplates(input))</c> into one <c>HashSet</c> under
/// structural <c>Word</c> equality, and an unordered <c>morphologicalRules</c> list compiles to a
/// <c>CombinationRuleCascade</c> that tries every rule at every step regardless of list position. When
/// both hold, the cascade already produces every word the template can produce, so the union is invariant
/// under any permutation of the template's slots and an adjacent swap is unobservable by construction. The
/// check is recomputed from the owning Stratum's and AffixTemplate's current attributes, never stored:
/// flipping the Stratum to <c>linear</c>, or dropping one of the template's rules from the cascade list,
/// invalidates every proof built from it.
/// </summary>
public static class TemplateMaskedProofs
{
    public const string Kind = "template-masked";

    /// <summary>Builds a <see cref="Kind"/> proof for <paramref name="item"/>, or null when a
    /// precondition fails.</summary>
    public static Proof? TryBuild(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);
        TemplateMaskedCheck check = Check(grammar, item);
        return check.IsMasked ? new Proof(item.Id, Kind, check.Reason) : null;
    }

    /// <summary>
    /// Re-verifies <paramref name="proof"/>: true only when <paramref name="fixtureId"/>'s freshly
    /// generated adjacent pairs still contain an item with <paramref name="proof"/>'s id AND the check
    /// recomputes masked for it. A stale id, a stratum flipped to <c>linear</c>, and a Slot rule dropped
    /// from the cascade all fail closed to false.
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
        return Check(grammar, item).IsMasked;
    }

    private sealed record TemplateMaskedCheck(string ItemId, bool IsMasked, string Reason);

    private static TemplateMaskedCheck Check(XDocument grammar, OrderingItem item)
    {
        if (item.Kind != OrderingListKind.AffixTemplateSlots)
        {
            return new TemplateMaskedCheck(
                item.Id,
                false,
                $"{item.Kind} pairs are not owned by an AffixTemplate's Slot list"
            );
        }

        XElement[] templates = grammar.Descendants("AffixTemplate").ToArray();
        if (item.OwnerOrdinal < 0 || item.OwnerOrdinal >= templates.Length)
        {
            return new TemplateMaskedCheck(
                item.Id,
                false,
                "owning AffixTemplate ordinal is out of range for the current document"
            );
        }

        XElement template = templates[item.OwnerOrdinal];
        XElement? stratum = template.Ancestors("Stratum").FirstOrDefault();
        if (stratum is null)
        {
            return new TemplateMaskedCheck(
                item.Id,
                false,
                $"AffixTemplate#{item.OwnerOrdinal} (\"{item.OwnerLabel}\") has no owning Stratum in the current document"
            );
        }

        // An absent morphologicalRuleOrder is the DTD default "linear" -- never treated as unordered.
        string order = (string?)stratum.Attribute("morphologicalRuleOrder") ?? "linear";
        if (order != "unordered")
        {
            return new TemplateMaskedCheck(
                item.Id,
                false,
                $"owning Stratum's morphologicalRuleOrder=\"{order}\", not unordered: a linear cascade does not "
                    + "already reproduce the template's slots regardless of position"
            );
        }

        var cascadeRules = SplitIdrefs((string?)stratum.Attribute("morphologicalRules"))
            .ToHashSet(StringComparer.Ordinal);

        // Every Slot of the WHOLE template, not merely the two being swapped -- the template applies its
        // slots in sequence, so a rule reachable only through a slot outside the pair still makes this
        // derivation path genuinely distinct from the cascade.
        List<XElement> slots = template.Elements("Slot").ToList();
        var missing = new List<string>();
        for (int i = 0; i < slots.Count; i++)
        {
            string? rulesAttribute = (string?)slots[i].Attribute("morphologicalRules");
            if (rulesAttribute is null)
            {
                return new TemplateMaskedCheck(
                    item.Id,
                    false,
                    $"Slot \"{SlotLabel(slots[i], i)}\" of AffixTemplate#{item.OwnerOrdinal} (\"{item.OwnerLabel}\") declares no "
                        + "morphologicalRules -- cannot verify it is reproduced by the cascade"
                );
            }

            foreach (string ruleId in SplitIdrefs(rulesAttribute))
            {
                if (!cascadeRules.Contains(ruleId))
                    missing.Add($"{ruleId} (Slot \"{SlotLabel(slots[i], i)}\")");
            }
        }

        if (missing.Count > 0)
        {
            return new TemplateMaskedCheck(
                item.Id,
                false,
                $"not every rule referenced by AffixTemplate#{item.OwnerOrdinal} (\"{item.OwnerLabel}\")'s Slots is a member of "
                    + $"the owning Stratum's morphologicalRules list -- missing {{{string.Join(",", missing)}}}: this template is "
                    + "a genuinely distinct derivation path, so its slot order can matter"
            );
        }

        return new TemplateMaskedCheck(
            item.Id,
            true,
            $"owning Stratum morphologicalRuleOrder=\"unordered\" and every rule referenced by every Slot of "
                + $"AffixTemplate#{item.OwnerOrdinal} (\"{item.OwnerLabel}\") is a member of its morphologicalRules list: the "
                + "compiled cascade already reproduces everything the template's slots can produce, so slot order is invariant"
        );
    }

    private static string SlotLabel(XElement slot, int index)
    {
        string? name = (string?)slot.Element("Name");
        return string.IsNullOrEmpty(name) ? $"Slot#{index}" : name;
    }

    private static IEnumerable<string> SplitIdrefs(string? value) =>
        string.IsNullOrEmpty(value)
            ? Enumerable.Empty<string>()
            : value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}
