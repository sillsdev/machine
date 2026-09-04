#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Assembles the combined Surface+Ordering coverage inventory -- 194 <see cref="CoverageItemKind.Surface"/>
/// items plus every Ordering adjacent pair the corpus declares -- from the two independent sweeps
/// (<see cref="CounterfactualLedger.Sweep"/> and <see cref="CounterfactualLedger.SweepOrdering"/>) into the
/// <see cref="CoverageItem"/>/<see cref="Evidence"/>/<see cref="Proof"/> shape
/// <see cref="CoverageCompletenessGate"/> consumes.
/// </summary>
public static class CoverageEvidencePipeline
{
    /// <summary>One item per surface result plus one per ordering result -- never merged or deduplicated,
    /// since a surface and an ordering pair are always distinct generators over disjoint id spaces.</summary>
    public static IReadOnlyList<CoverageItem> BuildItems(
        IReadOnlyList<CounterfactualResult> surfaceResults,
        IReadOnlyList<CounterfactualResult> orderingResults
    )
    {
        ArgumentNullException.ThrowIfNull(surfaceResults);
        ArgumentNullException.ThrowIfNull(orderingResults);
        var items = new List<CoverageItem>(surfaceResults.Count + orderingResults.Count);
        foreach (CounterfactualResult result in surfaceResults)
        {
            items.Add(
                new CoverageItem(
                    result.SurfaceId,
                    CoverageItemKind.Surface,
                    SurfaceOrigin(result.SurfaceId),
                    result.FixtureId
                )
            );
        }
        foreach (CounterfactualResult result in orderingResults)
        {
            items.Add(
                new CoverageItem(
                    result.SurfaceId,
                    CoverageItemKind.Ordering,
                    "adjacent-transposition",
                    result.FixtureId
                )
            );
        }
        return items.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
    }

    private static string SurfaceOrigin(string surfaceId) =>
        surfaceId.StartsWith(GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal) ? "dtd-enum" : "dtd-element";

    /// <summary>
    /// Every Ordering item the corpus currently declares, enumerated structurally from the checked-in
    /// grammars -- no engine sweep, no child process, just <see cref="OrderingGenerator.EnumerateAdjacentPairs"/>
    /// per fixture. Cheap enough to call on every test run, which is what lets a gap-count ratchet stay
    /// off the expensive sweep.
    /// </summary>
    public static IReadOnlyList<CoverageItem> BuildOrderingItems(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        var items = new List<CoverageItem>();
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            foreach (OrderingItem item in OrderingGenerator.EnumerateAdjacentPairs(grammar, fixture.Id))
                items.Add(new CoverageItem(item.Id, CoverageItemKind.Ordering, "adjacent-transposition", fixture.Id));
        }
        return items.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Evidence for every result whose verdict carries a real counter-example (Word or LoadFailure).</summary>
    public static IReadOnlyList<Evidence> BuildEvidence(IReadOnlyList<CounterfactualResult> results) =>
        results
            .Where(result => result.CounterexampleKind != CounterexampleKind.None)
            .Select(result => Evidence.FromCounterfactualResult(result.SurfaceId, result))
            .ToArray();

    /// <summary>
    /// Every proof the combined inventory can claim: the checked-in Surface impossibility claims
    /// (dtd-default, no-consumer, not-in-signature, blocked-by-defect), plus a freshly recomputed
    /// Ordering proof -- disjoint-domains, unordered-invariant, inactive-member, pos-disjoint,
    /// template-masked, never-fires, or feature-value-disjoint, tried in that order -- for every
    /// non-evidenced Ordering item one of them resolves. Ordering proofs are never read from a file --
    /// each kind's check only needs the checked-in grammar, so recomputing it here costs nothing worth
    /// caching and there is nothing that can go stale.
    /// </summary>
    public static IReadOnlyList<Proof> BuildProofs(
        string repositoryRoot,
        IReadOnlyList<CoverageItem> nonEvidencedOrderingItems
    )
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(nonEvidencedOrderingItems);

        var proofs = ImpossibilityProofs
            .Read(repositoryRoot)
            .Select(proof => new Proof(proof.SurfaceId, proof.Kind, proof.Evidence))
            .ToList();

        Dictionary<string, Fixture> fixturesById = Fixture
            .DiscoverAll(Path.Combine(repositoryRoot, "conformance"))
            .ToDictionary(fixture => fixture.Id, StringComparer.Ordinal);
        var grammarByFixture = new Dictionary<string, XDocument>(StringComparer.Ordinal);

        foreach (CoverageItem item in nonEvidencedOrderingItems)
        {
            if (!fixturesById.TryGetValue(item.Fixture, out Fixture? fixture))
                continue;
            if (!grammarByFixture.TryGetValue(item.Fixture, out XDocument? grammar))
            {
                grammar = XDocument.Load(fixture.GrammarPath);
                grammarByFixture[item.Fixture] = grammar;
            }

            OrderingItem? orderingItem = OrderingGenerator
                .EnumerateAdjacentPairs(grammar, item.Fixture)
                .FirstOrDefault(candidate => candidate.Id == item.Id);
            if (orderingItem is null)
                continue;

            Proof? proof =
                OrderingProofs.TryBuild(grammar, orderingItem)
                ?? UnorderedInvariantProofs.TryBuild(grammar, orderingItem)
                ?? InactiveMemberProofs.TryBuild(grammar, orderingItem)
                ?? PosDisjointProofs.TryBuild(grammar, orderingItem)
                ?? TemplateMaskedProofs.TryBuild(grammar, orderingItem)
                ?? NeverFiresProofs.TryBuild(grammar, orderingItem)
                ?? FeatureValueDisjointProofs.TryBuild(grammar, orderingItem);
            if (proof is not null)
                proofs.Add(proof);
        }

        return proofs;
    }

    /// <summary>A grammar loader keyed by fixture id, for <see cref="CoverageCompletenessGate.Evaluate"/>
    /// to re-verify disjoint-domains proofs against.</summary>
    public static Func<string, XDocument> GrammarLoader(string repositoryRoot)
    {
        Dictionary<string, Fixture> fixturesById = Fixture
            .DiscoverAll(Path.Combine(repositoryRoot, "conformance"))
            .ToDictionary(fixture => fixture.Id, StringComparer.Ordinal);
        return fixtureId => XDocument.Load(fixturesById[fixtureId].GrammarPath);
    }

    /// <summary>Every row the combined inventory should write to <see cref="EvidenceLedger"/>: one per
    /// item with real evidence.</summary>
    public static IReadOnlyList<EvidenceLedger.Row> BuildLedgerRows(
        IReadOnlyList<CoverageItem> items,
        IReadOnlyList<Evidence> evidence
    )
    {
        Dictionary<string, CoverageItem> itemsById = items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        return evidence.Select(e => EvidenceLedger.ToRow(itemsById[e.ItemId], e)).ToArray();
    }
}
