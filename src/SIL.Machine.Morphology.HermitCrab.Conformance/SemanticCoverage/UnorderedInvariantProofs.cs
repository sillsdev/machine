#nullable enable
using System;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Builds and re-verifies a <see cref="Proof"/> of kind <see cref="Kind"/>: a Stratum whose
/// <c>morphologicalRuleOrder</c> is <c>unordered</c> compiles to <c>CombinationRuleCascade</c> /
/// <c>ParallelCombinationRuleCascade</c> (<c>SynthesisStratumRule.cs</c>, <c>AnalysisStratumRule.cs</c>),
/// which tries every rule at every recursive step regardless of list position and collects into a
/// <c>HashSet</c> under structural <c>Word</c> equality. The reachable set is therefore invariant under
/// any permutation of that Stratum's <c>morphologicalRules</c> list, so an adjacent pair drawn from it can
/// never be order-sensitive. The check is recomputed from the owning Stratum's current attribute, never
/// stored, so flipping the stratum back to <c>linear</c> invalidates every proof built from it.
/// </summary>
public static class UnorderedInvariantProofs
{
    public const string Kind = "unordered-invariant";

    /// <summary>Builds a <see cref="Kind"/> proof for <paramref name="item"/>, or null when its owning
    /// Stratum is not unordered.</summary>
    public static Proof? TryBuild(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);
        UnorderedInvariantCheck check = Check(grammar, item);
        return check.IsUnordered ? new Proof(item.Id, Kind, check.Reason) : null;
    }

    /// <summary>
    /// Re-verifies <paramref name="proof"/>: true only when <paramref name="fixtureId"/>'s freshly
    /// generated adjacent pairs still contain an item with <paramref name="proof"/>'s id AND that item's
    /// current owning Stratum still declares <c>morphologicalRuleOrder="unordered"</c>. A stale id and a
    /// recomputed <c>linear</c>/absent order both fail closed to false.
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
        return Check(grammar, item).IsUnordered;
    }

    private sealed record UnorderedInvariantCheck(string ItemId, bool IsUnordered, string Reason);

    private static UnorderedInvariantCheck Check(XDocument grammar, OrderingItem item)
    {
        if (item.Kind != OrderingListKind.StratumMorphologicalRules)
        {
            return new UnorderedInvariantCheck(
                item.Id,
                false,
                $"{item.Kind} pairs are not owned by a Stratum's morphologicalRuleOrder"
            );
        }

        XElement[] strata = grammar.Descendants("Stratum").ToArray();
        if (item.OwnerOrdinal < 0 || item.OwnerOrdinal >= strata.Length)
        {
            return new UnorderedInvariantCheck(
                item.Id,
                false,
                "owning Stratum ordinal is out of range for the current document"
            );
        }

        string order = (string?)strata[item.OwnerOrdinal].Attribute("morphologicalRuleOrder") ?? "linear";
        return order == "unordered"
            ? new UnorderedInvariantCheck(
                item.Id,
                true,
                $"Stratum#{item.OwnerOrdinal} (\"{item.OwnerLabel}\") morphologicalRuleOrder=\"unordered\": "
                    + "the compiled cascade tries every rule at every step regardless of list position"
            )
            : new UnorderedInvariantCheck(
                item.Id,
                false,
                $"Stratum#{item.OwnerOrdinal} (\"{item.OwnerLabel}\") morphologicalRuleOrder=\"{order}\", not unordered"
            );
    }
}
