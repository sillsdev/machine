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

    // Pins the headline counts. 23 is FailureReason's own member count minus None (the enum this
    // ledger is keyed to, SIL.Machine.Morphology.HermitCrab/ITraceManager.cs); it moves only if that
    // enum itself gains or loses a member, never as a side effect of corpus changes. witnessed/
    // unreached come from EngineGateWitnessSweep's real traced run over every non-pathological,
    // non-crash fixture -- see UnreachedGatesAreTheKnownCorpusGaps below for why each of the 7 is
    // unreached. noDtdAttribute pins EngineGateInventoryLedger.DtdAttributes' six deliberate "-"
    // entries (three Pattern-family gates driven by PhoneticSequence element content, Environments and
    // DisjunctiveAllomorph driven by child elements/allomorph order rather than an attribute, and
    // SurfaceFormMismatch's pure engine-internal reconstruction check).
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
        Assert.That(witnessed, Is.EqualTo(16));
        Assert.That(unreached, Is.EqualTo(7));
        Assert.That(noDtdAttribute, Is.EqualTo(6));
        Assert.That(witnessed + unreached, Is.EqualTo(rows.Count));
    }

    // Every gate is a real enum member with at least one live TraceManager call site
    // (RaiseSiteScanner.Scan is mechanical, not hand-curated), so none can ever show "-" here --
    // unlike dtd_attributes and the witness columns, an empty raise_sites would mean the scanner
    // itself is broken, not a corpus gap.
    [Test]
    public void EveryGateHasAtLeastOneRaiseSite()
    {
        string root = RepositoryRoot();
        IReadOnlyList<EngineGateInventoryLedger.Row> rows = EngineGateInventoryLedger.Read(root);

        Assert.That(rows.All(r => r.RaiseSites != "-" && r.RaiseSites.Length > 0), Is.True);
    }

    // The exact 7 gates the current corpus never fires, each for a distinct, verified reason -- not
    // "the corpus is thin" but a specific missing shape: HeadPattern/NonHeadPattern need a compounding
    // candidate whose head/non-head phonetic shape is tried and fails to match (every compounding
    // fixture's candidates match by construction); MaxApplicationCount needs a rule re-applied to its
    // own multipleApplication cap within one derivation (no fixture repeats a rule that many times);
    // NonHeadProdRestrictMprFeatures needs analysis-direction compounding with a candidate root that
    // fails CompoundingRule.nonHeadProdRestrictionsMprFeatures (no fixture pairs that attribute with a
    // failing candidate); NonHeadRequiredSyntacticFeatureStruct needs a non-head candidate that fails
    // CompoundingRule.nonHeadPartsOfSpeech (the corpus's compounding fixtures exercise the head-side
    // gate -- HeadRequiredSyntacticFeatureStruct is Witnessed -- but never the non-head one failing);
    // NonPartialRuleRequiredAfterNonFinalTemplate needs a partial="true" rule tried right after an
    // AffixTemplate final="false" template (no fixture combines the two); ObligatorySyntacticFeatures
    // needs a rule's own outputObligatoryFeatures promise to go unmet by the time IsWordValid checks it
    // (no fixture's obligatory-feature rule ever produces a candidate missing that feature).
    [Test]
    public void UnreachedGatesAreTheKnownCorpusGaps()
    {
        string root = RepositoryRoot();
        IReadOnlyList<EngineGateInventoryLedger.Row> rows = EngineGateInventoryLedger.Read(root);
        string[] unreached = rows
            .Where(r => r.Status == EngineGateStatus.Unreached)
            .Select(r => r.Gate)
            .ToArray();

        Assert.That(
            unreached,
            Is.EquivalentTo(
                new[]
                {
                    "HeadPattern",
                    "MaxApplicationCount",
                    "NonHeadPattern",
                    "NonHeadProdRestrictMprFeatures",
                    "NonHeadRequiredSyntacticFeatureStruct",
                    "NonPartialRuleRequiredAfterNonFinalTemplate",
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

    // ------------------------------------------------------------------------------------------
    // RaiseSiteScanner: unit-level pins for the mechanical scan directly, independent of the real
    // corpus/engine, proving the "mechanical, not hand-curated" claim -- a real comparison
    // (`== FailureReason.None`) and a commented-out raise must never be counted, and a genuine raise
    // must always be found regardless of which of the 23 members it names.
    // ------------------------------------------------------------------------------------------

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
