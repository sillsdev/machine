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

    // Pins the headline counts against the CHECKED-IN ledger (GateObligationLedger.Compute runs a real
    // traced engine sweep plus severance re-parses -- exactly the cost EngineGateInventoryLedger.Compute
    // already pays -- so every assertion test here reads the checked-in file, per this directory's own
    // cost convention; only CheckedInGateObligationLedgerIsUpToDate below recomputes, and it is
    // [Explicit]). 23 gates x 2 arms (Blocked, Control) = 46. worth_covering is Yes for both arms of the
    // same 21 gates (SurfaceFormMismatch fails xml_reachable, ObligatorySyntacticFeatures fails
    // flex_producible -- see GateObligationLedger's own doc comment) = 42. Evidenced counts come from a
    // real severance + trace-attribution sweep over the current corpus (see gate-obligations.tsv itself
    // for exactly which word/fixture backs each one).
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
        // 9 -> 11 (2026-08-19): silaaku (languages/polysynthetic-stratal-derivation-chain) is a
        // genuine head-POS mismatch on mrCompoundHN (SILA, posN2, attempted as a head requiring
        // posN1), so severing CompoundingRule.headPartsOfSpeech -- the gate's own hardcoded
        // severance target -- actually flips it; akutat already supplied the Control witness.
        Assert.That(evidenced, Is.EqualTo(11));
        Assert.That(blockedEvidenced, Is.EqualTo(9));
        Assert.That(controlEvidenced, Is.EqualTo(2));
    }

    // Every gate contributes exactly one Blocked row and one Control row -- the denominator is gate x
    // arm, never a variable number of arms per gate the way the old chain-cell ledger has McDc vs
    // ConditionExtension vs Mutator counts that differ per chain.
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

    // worth_covering is a pure function of the other two layer verdicts, never an independent judgment
    // call -- if this ever drifts, the funnel this ledger reports stops meaning what its own doc
    // comment says it means.
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

    // The two known, documented layer blocks, pinned by name so a silent change in either upstream
    // ledger (engine-gate-inventory.tsv's DtdAttributes, fieldworks-producibility.tsv) is caught here
    // rather than only showing up as a funnel-count drift with no explanation.
    [Test]
    public void TheTwoNotWorthCoveringGatesFailTheDocumentedLayer()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        GateObligationLedger.Row surfaceFormMismatch = rows.First(r => r.Gate == "SurfaceFormMismatch" && r.Arm == "Blocked");
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

    // Every NotEvidenced row still names a real, non-empty reason -- the whole point of this ledger over
    // a bare "Unknown" collapse. A blank or placeholder evidence string here would silently defeat the
    // honesty requirement this ledger exists to satisfy.
    [Test]
    public void EveryRowCarriesNonEmptyEvidence()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        Assert.That(rows.All(r => !string.IsNullOrWhiteSpace(r.Evidence)), Is.True);
        Assert.That(rows.Any(r => r.Evidence.Trim() == "-"), Is.False);
    }

    // Pins exactly which (gate, arm) pairs are Evidenced today, and that each Evidenced row carries a
    // real fixture/word rather than the "-" placeholder reserved for NotEvidenced rows. This is the
    // regression net: if a future corpus/engine change makes one of these silently stop being
    // evidenced, this fails loudly instead of the funnel count quietly drifting.
    [Test]
    public void EvidencedRowsAreExactlyTheseElevenAndEachCarriesAFixtureAndWord()
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
                    ("ExcludedStemName", "Blocked"),
                    ("HeadProdRestrictMprFeatures", "Blocked"),
                    ("HeadProdRestrictMprFeatures", "Control"),
                    ("HeadRequiredSyntacticFeatureStruct", "Blocked"),
                    ("HeadRequiredSyntacticFeatureStruct", "Control"),
                    ("PartialParse", "Blocked"),
                    ("RequiredMprFeatures", "Blocked"),
                    ("RequiredStemName", "Blocked"),
                    ("RequiredSyntacticFeatureStruct", "Blocked"),
                }
            )
        );
        Assert.That(evidenced.All(r => r.Fixture != "-" && r.Word != "-"), Is.True);
    }

    // The two gates with BOTH arms evidenced today: HeadProdRestrictMprFeatures's blocking attribute
    // (CompoundingRule.headProdRestrictionsMprFeatures) and HeadRequiredSyntacticFeatureStruct's
    // (CompoundingRule.headPartsOfSpeech) both sit directly on a CompoundingRule element, so the
    // Control arm's rule-id resolution actually applies -- unlike every other Evidenced Blocked arm,
    // whose writer element (Allomorph/MorphologicalInput/AffixTemplate/PhonologicalSubrule) is not one
    // GrammarRuleIndex can resolve at all (see GateObligationLedger.RuleIndexedElements). This is the
    // "test the impossibility argument against an already-satisfied one" check: the SAME rule-indexed
    // mechanism that fails to resolve a Control arm for BoundRoot (Allomorph) succeeds for both
    // CompoundingRule-backed gates -- proving the Control arm's limitation is about the ELEMENT KIND,
    // not a broken mechanism.
    [Test]
    public void TwoGatesHaveBothArmsEvidenced()
    {
        string root = RepositoryRoot();
        IReadOnlyList<GateObligationLedger.Row> rows = GateObligationLedger.Read(root);

        string[] gatesWithBothArmsEvidenced = rows
            .Where(r => r.Status == GateArmStatus.Evidenced)
            .GroupBy(r => r.Gate)
            .Where(g => g.Count() == 2)
            .Select(g => g.Key)
            .ToArray();

        Assert.That(
            gatesWithBothArmsEvidenced,
            Is.EquivalentTo(new[] { "HeadProdRestrictMprFeatures", "HeadRequiredSyntacticFeatureStruct" })
        );

        GateObligationLedger.Row boundRootControl = rows.First(r => r.Gate == "BoundRoot" && r.Arm == "Control");
        Assert.That(boundRootControl.Status, Is.EqualTo(GateArmStatus.NotEvidenced));
        Assert.That(boundRootControl.Evidence, Does.Contain("Allomorph is not a rule element"));

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
    }

    // Mirrors EngineGateInventoryLedgerTests.CheckedInEngineGateInventoryLedgerIsUpToDate: recomputing
    // this ledger re-runs a traced engine sweep (one parse per word per triggering fixture) plus a
    // severance re-parse per candidate -- real engine cost, not something the default suite should pay
    // more than once. Every other test in this file reads the checked-in ledger; only this one recomputes.
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
