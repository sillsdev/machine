using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class EngineGateInventoryLedgerTests
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

    [Test]
    public void RealCorpusProducesTheDeclaredGateCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<EngineGateInventoryLedger.Row> rows = EngineGateInventoryLedger.Read(root);

        int witnessed = rows.Count(r => r.Status == EngineGateStatus.Witnessed);
        int unreached = rows.Count(r => r.Status == EngineGateStatus.Unreached);
        int noDtdAttribute = rows.Count(r => r.DtdAttributes == "-");

        TestContext.Out.WriteLine(
            $"gates={rows.Count} witnessed={witnessed} unreached={unreached} noDtdAttribute={noDtdAttribute}"
        );

        Assert.That(rows, Has.Count.EqualTo(23));
        Assert.That(witnessed, Is.EqualTo(18));
        Assert.That(unreached, Is.EqualTo(5));
        Assert.That(noDtdAttribute, Is.EqualTo(6));
        Assert.That(witnessed + unreached, Is.EqualTo(rows.Count));
    }

    [Test]
    public void EveryGateHasAtLeastOneRaiseSite()
    {
        string root = RepositoryRoot();
        IReadOnlyList<EngineGateInventoryLedger.Row> rows = EngineGateInventoryLedger.Read(root);

        Assert.That(rows.All(r => r.RaiseSites != "-" && r.RaiseSites.Length > 0), Is.True);
    }

    // These gates have no firing fixture in the current corpus.
    [Test]
    public void UnreachedGatesAreTheKnownCorpusGaps()
    {
        string root = RepositoryRoot();
        IReadOnlyList<EngineGateInventoryLedger.Row> rows = EngineGateInventoryLedger.Read(root);
        string[] unreached = rows.Where(r => r.Status == EngineGateStatus.Unreached).Select(r => r.Gate).ToArray();

        Assert.That(
            unreached,
            Is.EquivalentTo(
                new[]
                {
                    "HeadPattern",
                    "NonHeadPattern",
                    "NonHeadProdRestrictMprFeatures",
                    "NonHeadRequiredSyntacticFeatureStruct",
                    "ObligatorySyntacticFeatures",
                }
            )
        );
    }

    // Mirrors DataflowObligationLedgerTests.CheckedInDataflowObligationLedgerIsUpToDate: regenerate and
    // require the checked-in file to match byte for byte.
    [Explicit("Runs a traced sweep of every fixture word; the checked-in ledger is what the other tests read.")]
    [Test]
    public void CheckedInEngineGateInventoryLedgerIsUpToDate()
    {
        string root = RepositoryRoot();
        IReadOnlyList<EngineGateInventoryLedger.Row> rows = EngineGateInventoryLedger.Compute(root);

        string fresh = EngineGateInventoryLedger.ToText(rows);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, EngineGateInventoryLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            fresh.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-engine-gate-inventory --repository-root ."
        );
    }

    [Test]
    public void ScanFindsARaiseSiteButSkipsComparisonsAndComments()
    {
        string root = Path.Combine(Path.GetTempPath(), "engine-gate-scanner-test-" + Path.GetRandomFileName());
        string engineDir = Path.Combine(root, "src", "SIL.Machine.Morphology.HermitCrab");
        Directory.CreateDirectory(engineDir);
        File.WriteAllLines(
            Path.Combine(engineDir, "Sample.cs"),
            new[]
            {
                "// a commented-out raise: FailureReason.BoundRoot,",
                "if (trace.FailureReason == FailureReason.None) return;",
                "if (reason == FailureReason.MaxApplicationCount) { }",
                "morpher.TraceManager.Failed(lang, word, FailureReason.BoundRoot, this, null);",
            }
        );

        try
        {
            IReadOnlyDictionary<string, IReadOnlyList<string>> sites = RaiseSiteScanner.Scan(root);

            Assert.That(sites.ContainsKey("None"), Is.False);
            Assert.That(sites.ContainsKey("MaxApplicationCount"), Is.False);
            Assert.That(sites["BoundRoot"], Is.EqualTo(new[] { "Sample.cs:4" }));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
