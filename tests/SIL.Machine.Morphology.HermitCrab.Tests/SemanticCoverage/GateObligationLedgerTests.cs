using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class GateObligationLedgerTests
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
    public void CheckedInLedgerHasTheDeclaredCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        int gates = rows.Select(r => r.Gate).Distinct().Count();
        int worthCovering = rows.Count(r => r.WorthCovering == "Yes");
        int evidenced = rows.Count(r => r.Status == GateArmStatus.Evidenced);
        int notEvidenced = rows.Count(r => r.Status == GateArmStatus.NotEvidenced);
        int blockedEvidenced = rows.Count(r => r.Arm == "Blocked" && r.Status == GateArmStatus.Evidenced);
        int controlEvidenced = rows.Count(r => r.Arm == "Control" && r.Status == GateArmStatus.Evidenced);

        TestContext.Out.WriteLine(
            $"gates={gates} rows={rows.Count} worthCovering={worthCovering} evidenced={evidenced} "
                + $"notEvidenced={notEvidenced} blockedEvidenced={blockedEvidenced} controlEvidenced={controlEvidenced}"
        );

        Assert.That(rows, Has.Count.EqualTo(46));
        Assert.That(gates, Is.EqualTo(23));
        Assert.That(worthCovering, Is.EqualTo(42));
        Assert.That(evidenced + notEvidenced, Is.EqualTo(rows.Count));
        Assert.That(evidenced, Is.EqualTo(16));
        Assert.That(blockedEvidenced, Is.EqualTo(10));
        Assert.That(controlEvidenced, Is.EqualTo(6));
    }

    [Test]
    public void EveryGateHasExactlyOneBlockedAndOneControlRow()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        foreach (var byGate in rows.GroupBy(r => r.Gate))
        {
            Assert.That(byGate.Count(), Is.EqualTo(2), $"{byGate.Key} must have exactly 2 rows");
            Assert.That(byGate.Select(r => r.Arm), Is.EquivalentTo(new[] { "Blocked", "Control" }), byGate.Key);
        }
    }

    [Test]
    public void WorthCoveringIsExactlyXmlReachableAndFlexProducible()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        foreach (GateObligationLedger.Row row in rows)
        {
            string expected = row.XmlReachable == "Yes" && row.FlexProducible == "Yes" ? "Yes" : "No";
            Assert.That(row.WorthCovering, Is.EqualTo(expected), row.Gate + "/" + row.Arm);
        }
    }

    [Test]
    public void TheTwoNotWorthCoveringGatesFailTheDocumentedLayer()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        GateObligationLedger.Row surfaceFormMismatch = rows.First(r =>
            r.Gate == "SurfaceFormMismatch" && r.Arm == "Blocked"
        );
        Assert.That(surfaceFormMismatch.XmlReachable, Is.EqualTo("No"));
        Assert.That(surfaceFormMismatch.FlexProducible, Is.EqualTo("Yes"));
        Assert.That(surfaceFormMismatch.WorthCovering, Is.EqualTo("No"));

        GateObligationLedger.Row obligatorySyntacticFeatures = rows.First(r =>
            r.Gate == "ObligatorySyntacticFeatures" && r.Arm == "Blocked"
        );
        Assert.That(obligatorySyntacticFeatures.XmlReachable, Is.EqualTo("Yes"));
        Assert.That(obligatorySyntacticFeatures.FlexProducible, Is.EqualTo("No"));
        Assert.That(obligatorySyntacticFeatures.WorthCovering, Is.EqualTo("No"));

        GateObligationLedger.Row[] notWorthCovering = rows.Where(r => r.WorthCovering == "No").ToArray();
        Assert.That(
            notWorthCovering.Select(r => r.Gate).Distinct(),
            Is.EquivalentTo(new[] { "SurfaceFormMismatch", "ObligatorySyntacticFeatures" })
        );
    }

    [Test]
    public void EveryRowCarriesNonEmptyEvidence()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        Assert.That(rows.All(r => !string.IsNullOrWhiteSpace(r.Evidence)), Is.True);
        Assert.That(rows.Any(r => r.Evidence.Trim() == "-"), Is.False);
    }

    [Test]
    public void EvidencedRowsAreExactlyTheseSixteenAndEachCarriesAFixtureAndWord()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);
        GateObligationLedger.Row[] evidenced = rows.Where(r => r.Status == GateArmStatus.Evidenced).ToArray();

        Assert.That(
            evidenced.Select(r => (r.Gate, r.Arm)),
            Is.EquivalentTo(
                new[]
                {
                    ("BoundRoot", "Blocked"),
                    ("ExcludedMprFeatures", "Blocked"),
                    ("ExcludedMprFeatures", "Control"),
                    ("ExcludedStemName", "Blocked"),
                    ("HeadProdRestrictMprFeatures", "Blocked"),
                    ("HeadProdRestrictMprFeatures", "Control"),
                    ("HeadRequiredSyntacticFeatureStruct", "Blocked"),
                    ("HeadRequiredSyntacticFeatureStruct", "Control"),
                    ("NonPartialRuleRequiredAfterNonFinalTemplate", "Blocked"),
                    ("NonPartialRuleRequiredAfterNonFinalTemplate", "Control"),
                    ("PartialParse", "Blocked"),
                    ("RequiredMprFeatures", "Blocked"),
                    ("RequiredMprFeatures", "Control"),
                    ("RequiredStemName", "Blocked"),
                    ("RequiredSyntacticFeatureStruct", "Blocked"),
                    ("RequiredSyntacticFeatureStruct", "Control"),
                }
            )
        );
        Assert.That(evidenced.All(r => r.Fixture != "-" && r.Word != "-"), Is.True);
    }

    [Test]
    public void SixGatesHaveBothArmsEvidenced()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        string[] gatesWithBothArmsEvidenced = rows.Where(r => r.Status == GateArmStatus.Evidenced)
            .GroupBy(r => r.Gate)
            .Where(g => g.Count() == 2)
            .Select(g => g.Key)
            .ToArray();

        Assert.That(
            gatesWithBothArmsEvidenced,
            Is.EquivalentTo(
                new[]
                {
                    "ExcludedMprFeatures",
                    "HeadProdRestrictMprFeatures",
                    "HeadRequiredSyntacticFeatureStruct",
                    "NonPartialRuleRequiredAfterNonFinalTemplate",
                    "RequiredMprFeatures",
                    "RequiredSyntacticFeatureStruct",
                }
            )
        );

        GateObligationLedger.Row boundRootControl = rows.First(r => r.Gate == "BoundRoot" && r.Arm == "Control");
        Assert.That(boundRootControl.Status, Is.EqualTo(GateArmStatus.NotEvidenced));
        Assert.That(boundRootControl.Evidence, Does.Contain("no rule-element ancestor"));

        GateObligationLedger.Row headProdControl = rows.First(r =>
            r.Gate == "HeadProdRestrictMprFeatures" && r.Arm == "Control"
        );
        Assert.That(headProdControl.Status, Is.EqualTo(GateArmStatus.Evidenced));
        Assert.That(headProdControl.Evidence, Does.Contain("CompoundingRule"));

        GateObligationLedger.Row headRequiredControl = rows.First(r =>
            r.Gate == "HeadRequiredSyntacticFeatureStruct" && r.Arm == "Control"
        );
        Assert.That(headRequiredControl.Status, Is.EqualTo(GateArmStatus.Evidenced));
        Assert.That(headRequiredControl.Evidence, Does.Contain("CompoundingRule"));

        GateObligationLedger.Row excludedMprControl = rows.First(r =>
            r.Gate == "ExcludedMprFeatures" && r.Arm == "Control"
        );
        Assert.That(excludedMprControl.Status, Is.EqualTo(GateArmStatus.Evidenced));
        Assert.That(excludedMprControl.Evidence, Does.Contain("MorphologicalInput"));

        GateObligationLedger.Row requiredSyntacticControl = rows.First(r =>
            r.Gate == "RequiredSyntacticFeatureStruct" && r.Arm == "Control"
        );
        Assert.That(requiredSyntacticControl.Status, Is.EqualTo(GateArmStatus.Evidenced));
        Assert.That(requiredSyntacticControl.Evidence, Does.Contain("TraceRuleAttributor"));
    }

    [Explicit("Runs a traced engine sweep plus severance re-parses across every gate's triggering fixtures.")]
    [Test]
    public void CheckedInGateObligationLedgerIsUpToDate()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Compute(root);

        string fresh = GateObligationLedger.ToText(rows);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, GateObligationLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            fresh.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-gate-obligations --repository-root ."
        );
    }
}
