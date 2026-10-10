using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Pins the corpus-wide gap count -- items neither evidenced (conformance/semantic-coverage-evidence.tsv)
/// nor proven (conformance/semantic-coverage-proofs.tsv plus a fresh, cheap disjoint-domains/unordered-
/// invariant/inactive-member/pos-disjoint recompute for Ordering) -- without recomputing the expensive
/// sweep: every input here is either a checked-in file or a pure, engine-free grammar read. Gaps may only
/// go down. A gate that fails on every existing gap would block all ordinary work and get switched off, so
/// this is a ratchet: it fails only if the count goes UP.
/// </summary>
[TestFixture]
public sealed class CoverageGapRatchetTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    private const int PinnedGapCount = 25;

    [Test]
    public void CorpusWideGapCountNeverIncreasesFromThePinnedValue()
    {
        string root = RepositoryRoot();

        // The evidenced set: read, never recomputed -- this is what makes the test cheap.
        IReadOnlyList<EvidenceLedger.Row> evidenceLedger = EvidenceLedger.Read(root);
        var evidencedIds = evidenceLedger.Select(row => row.ItemId).ToHashSet(StringComparer.Ordinal);

        IReadOnlyList<CounterfactualResult> surfaceResults = CounterfactualLedger.Read(root);
        IReadOnlyList<CoverageItem> orderingItems = CoverageEvidencePipeline.BuildOrderingItems(root);

        Assert.That(surfaceResults, Is.Not.Empty, "run --write-counterfactual first to populate the Surface ledger");
        Assert.That(orderingItems, Is.Not.Empty, "the corpus must declare at least one Ordering item");

        IReadOnlyList<CoverageItem> items = CoverageEvidencePipeline
            .BuildItems(surfaceResults, Array.Empty<CounterfactualResult>())
            .Concat(orderingItems)
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        var itemsById = items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        string[] orphanedEvidenceRows = evidenceLedger
            .Select(row => row.ItemId)
            .Where(id => !itemsById.ContainsKey(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        Assert.That(
            orphanedEvidenceRows,
            Is.Empty,
            "evidence rows outside the generated inventory must fail independently of the gap pin"
        );
        IReadOnlyList<Evidence> evidence = evidenceLedger
            .Select(row => EvidenceLedger.ToEvidence(row, itemsById[row.ItemId]))
            .ToArray();
        CoverageItem[] nonEvidencedOrdering = orderingItems.Where(item => !evidencedIds.Contains(item.Id)).ToArray();
        IReadOnlyList<Proof> proofs = CoverageEvidencePipeline.BuildProofs(root, nonEvidencedOrdering);
        CompletenessReport completeness = CoverageCompletenessGate.Evaluate(
            items,
            evidence,
            proofs,
            CoverageEvidencePipeline.GrammarLoader(root)
        );
        Assert.That(
            completeness.OrphanedEvidenceItemIds,
            Is.Empty,
            "evidence outside the generated inventory must fail independently of the gap pin"
        );
        Assert.That(
            completeness.Items.Where(result => result.Resolution == CoverageResolution.Conflicting),
            Is.Empty,
            "evidence/proof conflicts are stale claims, not gaps the numeric ratchet may absorb"
        );
        Assert.That(
            completeness.OrphanedProofItemIds,
            Is.Empty,
            "proofs outside the generated inventory must fail independently of the gap pin"
        );
        var kindByItemId = items.ToDictionary(item => item.Id, item => item.Kind, StringComparer.Ordinal);
        int surfaceGaps = completeness.Items.Count(result =>
            result.Resolution is CoverageResolution.Unresolved or CoverageResolution.Rejected
            && kindByItemId[result.ItemId] == CoverageItemKind.Surface
        );
        int orderingGaps = completeness.Items.Count(result =>
            result.Resolution is CoverageResolution.Unresolved or CoverageResolution.Rejected
            && kindByItemId[result.ItemId] == CoverageItemKind.Ordering
        );
        int totalGaps = surfaceGaps + orderingGaps;

        TestContext.Out.WriteLine(
            $"total items: {surfaceResults.Count + orderingItems.Count} "
                + $"(Surface {surfaceResults.Count}, Ordering {orderingItems.Count})"
        );
        TestContext.Out.WriteLine(
            $"gaps: {totalGaps} (Surface {surfaceGaps}, Ordering {orderingGaps}); pinned at {PinnedGapCount}"
        );
        foreach (
            var group in completeness
                .Items.Where(result => result.Resolution == CoverageResolution.Proven)
                .GroupBy(result => result.Detail.Split(':')[0])
                .OrderByDescending(group => group.Count())
        )
        {
            TestContext.Out.WriteLine($"  proven ({group.Key}): {group.Count()}");
        }
        TestContext.Out.WriteLine(
            $"  rejected: {completeness.Items.Count(result => result.Resolution == CoverageResolution.Rejected)}"
        );
        TestContext.Out.WriteLine(
            $"  unresolved: {completeness.Items.Count(result => result.Resolution == CoverageResolution.Unresolved)}"
        );

        Assert.That(
            totalGaps,
            Is.LessThanOrEqualTo(PinnedGapCount),
            $"corpus-wide gaps increased from the pinned {PinnedGapCount} to {totalGaps}; coverage regressed"
        );
        if (totalGaps < PinnedGapCount)
        {
            Assert.Warn(
                $"corpus-wide gaps decreased from the pinned {PinnedGapCount} to {totalGaps}; "
                    + $"lower CoverageGapRatchetTests.PinnedGapCount to {totalGaps}."
            );
        }
    }
}
