#nullable enable
using System;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Builds and re-verifies a <see cref="Proof"/> of kind <see cref="Kind"/> from
/// <see cref="OrderingGenerator.CheckDisjointDomains"/> rather than from hand-written prose. A
/// disjoint-domains claim is about the GRAMMAR -- the earlier rule's effect (output plus its own input)
/// cannot reach anything the later rule is sensitive to (its input plus its Environment templates) --
/// never about which words a fixture happens to contain, so both directions here call the same
/// recomputation the design doc names as authoritative.
/// </summary>
public static class OrderingProofs
{
    public const string Kind = "disjoint-domains";

    /// <summary>
    /// Builds a <see cref="Kind"/> proof for <paramref name="item"/> from
    /// <see cref="OrderingGenerator.CheckDisjointDomains"/>'s own reasoning, or null when the check does
    /// not return <see cref="DomainRelation.Disjoint"/> -- <see cref="DomainRelation.Overlaps"/> and
    /// <see cref="DomainRelation.Undetermined"/> can never license this proof kind.
    /// </summary>
    public static Proof? TryBuild(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);
        DisjointDomainsCheck check = OrderingGenerator.CheckDisjointDomains(grammar, item);
        return check.Relation == DomainRelation.Disjoint ? new Proof(item.Id, Kind, check.Reason) : null;
    }

    /// <summary>
    /// Re-verifies <paramref name="proof"/> against <paramref name="grammar"/>: true only when
    /// <paramref name="fixtureId"/>'s freshly generated adjacent pairs still contain an item with
    /// <paramref name="proof"/>'s id AND <see cref="OrderingGenerator.CheckDisjointDomains"/> recomputes
    /// <see cref="DomainRelation.Disjoint"/> for it. A stale id (the swap no longer matches the document)
    /// and a recomputed <see cref="DomainRelation.Overlaps"/>/<see cref="DomainRelation.Undetermined"/>
    /// both fail closed to false rather than passing on nothing but the claim itself.
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
        return OrderingGenerator.CheckDisjointDomains(grammar, item).Relation == DomainRelation.Disjoint;
    }
}
