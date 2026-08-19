using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Recomputes the whole Surface+Ordering sweep and checks it against the checked-in
/// conformance/semantic-coverage-evidence.tsv, the same protection
/// <see cref="CounterfactualGateTests.TheCheckedInLedgerMatchesAFreshRecomputeExactly"/> already gives the
/// legacy Surface-only ledger. A stale evidence ledger silently understates the gap count
/// <see cref="CoverageGapRatchetTests"/> ratchets on.
/// </summary>
[TestFixture]
public sealed class EvidenceLedgerFreshnessTests
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

    // Recomputes both sweeps end to end: re-parses every fixture once per Surface plus once per Ordering
    // adjacent pair. Costs minutes, same order as the legacy Surface-only freshness test; run manually or
    // by the coverage-evidence workflow, never on every push.
    [Test]
    [Explicit("re-parses every fixture once per Surface item plus once per Ordering adjacent pair")]
    [Category("Counterfactual")]
    public void TheCheckedInEvidenceLedgerMatchesAFreshRecomputeExactly()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);

        IReadOnlyList<CounterfactualResult> freshSurface = CounterfactualLedger.Sweep(root, inventory);
        IReadOnlyList<CounterfactualResult> freshOrdering = CounterfactualLedger.SweepOrdering(root);
        IReadOnlyList<CoverageItem> items = CoverageEvidencePipeline.BuildItems(freshSurface, freshOrdering);
        IReadOnlyList<Evidence> evidence = CoverageEvidencePipeline.BuildEvidence(freshSurface.Concat(freshOrdering).ToArray());
        IReadOnlyList<EvidenceLedger.Row> fresh = CoverageEvidencePipeline.BuildLedgerRows(items, evidence);

        IReadOnlyList<EvidenceLedger.Row> checkedIn = EvidenceLedger.Read(root);

        var freshById = fresh.ToDictionary(row => row.ItemId, StringComparer.Ordinal);
        var checkedInById = checkedIn.ToDictionary(row => row.ItemId, StringComparer.Ordinal);

        string[] added = freshById
            .Keys.Except(checkedInById.Keys, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] removed = checkedInById
            .Keys.Except(freshById.Keys, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] changed = freshById
            .Keys.Intersect(checkedInById.Keys, StringComparer.Ordinal)
            .Where(id => freshById[id] != checkedInById[id])
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                added,
                Is.Empty,
                $"newly evidenced items; regenerate with --write-coverage-evidence:\n  {string.Join("\n  ", added)}"
            );
            Assert.That(
                removed,
                Is.Empty,
                $"stale rows; delete from {EvidenceLedger.RelativePath}:\n  {string.Join("\n  ", removed)}"
            );
            Assert.That(
                changed,
                Is.Empty,
                $"changed evidence; regenerate with --write-coverage-evidence after checking why:\n  {string.Join("\n  ", changed)}"
            );
        });
    }
}
