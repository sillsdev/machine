using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class GrammarCoverageLedgerTests
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

    // Every one of the 35 fixtures appears at least once -- a fixture with zero rows here would mean
    // the per-grammar view has nothing to say about it at all, which is exactly the gap this ledger
    // exists to close. Layer totals are pinned so a source ledger drifting silently changes this file
    // in a way CheckedInGrammarCoverageLedgerIsUpToDate below would also catch.
    //
    // Surface dropped 191 -> 189 (rows 641 -> 639) on a full --write-counterfactual +
    // --write-coverage-evidence re-sweep: BoundaryMarker and Gloss no longer reach Evidenced in any
    // fixture (their sole prior witness, since neither is checked in EvidenceLedger anymore). 33 ->
    // 35 fixtures (rows 647 -> 664) when rewrite-analysis-feature-neutralization/
    // synthesis-stratum-render-stale-table were added.
    [Test]
    public void CheckedInLedgerCoversAllFixturesWithTheMeasuredLayerCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GrammarCoverageLedger.Row> rows = GrammarCoverageLedger.Read(root);

        int distinctFixtures = rows.Select(r => r.Fixture).Distinct().Count();
        int surface = rows.Count(r => r.Layer == ObligationLayer.Surface);
        int @interface = rows.Count(r => r.Layer == ObligationLayer.Interface);
        int construct = rows.Count(r => r.Layer == ObligationLayer.Construct);

        TestContext.Out.WriteLine(
            $"rows={rows.Count} fixtures={distinctFixtures} surface={surface} interface={@interface} construct={construct}"
        );

        Assert.That(rows, Has.Count.EqualTo(664));
        Assert.That(distinctFixtures, Is.EqualTo(35));
        Assert.That(surface, Is.EqualTo(189));
        Assert.That(@interface, Is.EqualTo(383));
        Assert.That(construct, Is.EqualTo(92));
    }

    // This is a JOIN over three already-checked-in ledgers (EvidenceLedger, InterfaceInventoryLedger +
    // InterfaceWitnessLedger, ConstructClaimCorroboration) -- no reparse of its own, so unlike
    // InterfaceWitnessLedgerTests's freshness test this one is cheap enough to run on every pass.
    [Test]
    public void CheckedInGrammarCoverageLedgerIsUpToDate()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GrammarCoverageLedger.Row> fresh = GrammarCoverageLedger.Compute(root);
        string freshText = GrammarCoverageLedger.ToText(fresh);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, GrammarCoverageLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            freshText.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-coverage-traceability --repository-root ."
        );
    }
}
