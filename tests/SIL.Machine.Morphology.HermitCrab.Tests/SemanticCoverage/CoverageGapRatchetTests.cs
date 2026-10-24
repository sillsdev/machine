using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Pins the exact open coverage debt by item ID and resolution. New gaps fail, and a resolved item
/// must be removed from the named baseline; the baseline is not an exception list. Inputs are either
/// checked-in files or pure, engine-free grammar reads, so the test avoids recomputing the expensive
/// counterfactual sweep.
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

    private static IReadOnlyDictionary<string, CoverageResolution> ReadGapBaseline(string root)
    {
        string path = Path.Combine(root, "conformance", "semantic-coverage-gap-baseline.tsv");
        Assert.That(File.Exists(path), Is.True, $"missing named open-debt baseline: {path}");
        var gaps = new Dictionary<string, CoverageResolution>(StringComparer.Ordinal);
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (
                line.Length == 0
                || line.StartsWith("#", StringComparison.Ordinal)
                || line.StartsWith("item_id\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            Assert.That(fields, Has.Length.EqualTo(2), $"malformed named coverage debt row in {path}: {line}");
            bool known = Enum.TryParse(fields[1], out CoverageResolution resolution);
            Assert.That(known, Is.True, $"unknown coverage resolution in {path}: {fields[1]}");
            Assert.That(
                resolution,
                Is.AnyOf(CoverageResolution.Unresolved, CoverageResolution.Rejected),
                $"only open unresolved/rejected coverage debt belongs in {path}: {line}"
            );
            Assert.That(gaps.TryAdd(fields[0], resolution), Is.True, $"duplicate item ID in {path}: {fields[0]}");
        }
        return gaps;
    }

    [Test]
    public void OpenCoverageDebtMatchesTheNamedItemAndResolutionBaselineExactly()
    {
        string root = RepositoryRoot();
        IReadOnlyDictionary<string, CoverageResolution> expectedGaps = ReadGapBaseline(root);

        IReadOnlyList<EvidenceLedger.Row> evidenceLedger = EvidenceLedger.Read(root);
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
            "evidence rows outside the generated inventory must fail independently of the named debt baseline"
        );
        IReadOnlyList<Evidence> evidence = evidenceLedger
            .Select(row => EvidenceLedger.ToEvidence(row, itemsById[row.ItemId]))
            .ToArray();
        var evidencedIds = evidenceLedger.Select(row => row.ItemId).ToHashSet(StringComparer.Ordinal);
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
            "evidence outside the generated inventory must fail independently of the named debt baseline"
        );
        Assert.That(
            completeness.Items.Where(result => result.Resolution == CoverageResolution.Conflicting),
            Is.Empty,
            "evidence/proof conflicts are stale claims, not coverage debt"
        );
        Assert.That(
            completeness.OrphanedProofItemIds,
            Is.Empty,
            "proofs outside the generated inventory must fail independently of the named debt baseline"
        );

        CoverageResolution[] debtResolutions = { CoverageResolution.Unresolved, CoverageResolution.Rejected };
        CoverageResolutionResult[] actualGaps = completeness
            .Items.Where(result => debtResolutions.Contains(result.Resolution))
            .OrderBy(result => result.ItemId, StringComparer.Ordinal)
            .ToArray();
        var actualById = actualGaps.ToDictionary(
            result => result.ItemId,
            result => result.Resolution,
            StringComparer.Ordinal
        );
        string[] unexpectedOrChanged = actualById
            .Where(pair =>
                !expectedGaps.TryGetValue(pair.Key, out CoverageResolution expected) || expected != pair.Value
            )
            .Select(pair => $"{pair.Key}\t{pair.Value}")
            .ToArray();
        string[] resolvedOrChanged = expectedGaps
            .Where(pair => !actualById.TryGetValue(pair.Key, out CoverageResolution actual) || actual != pair.Value)
            .Select(pair => $"{pair.Key}\t{pair.Value}")
            .ToArray();
        var kindByItemId = items.ToDictionary(item => item.Id, item => item.Kind, StringComparer.Ordinal);
        int surfaceGaps = actualGaps.Count(result => kindByItemId[result.ItemId] == CoverageItemKind.Surface);
        int orderingGaps = actualGaps.Length - surfaceGaps;

        TestContext.Out.WriteLine(
            $"total items: {surfaceResults.Count + orderingItems.Count} (Surface {surfaceResults.Count}, Ordering {orderingItems.Count})"
        );
        TestContext.Out.WriteLine(
            $"open coverage debt: {actualGaps.Length} (Surface {surfaceGaps}, Ordering {orderingGaps}); "
                + $"named item/resolution baseline {expectedGaps.Count}"
        );
        TestContext.Out.WriteLine(
            $"  unresolved: {actualGaps.Count(result => result.Resolution == CoverageResolution.Unresolved)}"
        );
        TestContext.Out.WriteLine(
            $"  rejected: {actualGaps.Count(result => result.Resolution == CoverageResolution.Rejected)}"
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
        Assert.Multiple(() =>
        {
            Assert.That(
                unexpectedOrChanged,
                Is.Empty,
                $"new or changed coverage debt; investigate the item, update its evidence/proof, or review the baseline after an audit:\n  {string.Join("\n  ", unexpectedOrChanged)}"
            );
            Assert.That(
                resolvedOrChanged,
                Is.Empty,
                $"baseline entries no longer match open coverage debt; remove resolved pairs after verification:\n  {string.Join("\n  ", resolvedOrChanged)}"
            );
        });
    }
}
