using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class FoldInCandidateLedgerTests
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

    // The primary deliverable this whole traceability build was for. Interface layer: WITNESS-based
    // analysis finds only 1 interface (MorphologicalInput.excludedMPRFeatures) witnessed only by a
    // fabricated edge case -- down from the presence-based count of 2 the original brief cited
    // (SymbolicFeature.defaultSymbol drops out entirely: DefaultSymbolIsPresentButNeverWitnessed in
    // InterfaceWitnessLedgerTests shows it is never witnessed ANYWHERE, so it was never a genuine
    // "edge-case-only" case, just an artifact of counting presence as coverage). 27 more interfaces are
    // present somewhere but witnessed nowhere -- not fold-in candidates, but a named gap. Surface
    // layer: 57 unit surfaces are clean fold-in candidates (witnessed only by an edge case, and no
    // language grammar even structurally contains them).
    //
    // surfacePresentElsewhere dropped 30 -> 8: CounterfactualLedger.Sweep used to keep only the
    // first-discovered fixture per surface on a verdict tie, and Fixture.DiscoverAll sorts
    // edge-cases/* before languages/*, so a languages/* fixture that merely TIED for the same verdict
    // was invisible -- 22 of the 30 were actually already witnessed by a language grammar, just masked
    // by the tie-break. Fixed by recording every tied fixture (CounterfactualResult.WitnessingFixtures,
    // the ledger's witnessed_by column) and having FoldInCandidateLedger check that full set instead of
    // the single recorded fixture.
    //
    // 92 -> 91 (interfaceEdgeCaseOnly 2 -> 1) after languages/fusional-realizational-morphology's
    // family-blocking fieldworks_producible conversion (this branch) moved mrPast2's
    // excludedMPRFeatures reader into a new AffixTemplate slot: MorphologicalInput.excludedMPRFeatures
    // is now present in a `languages/*` grammar, not just the edge-cases-only fixtures that used to be
    // its sole witnesses, so it is no longer an edge-case-only interface. The other pinned edge case,
    // CompoundingRule.outputPartOfSpeech, is untouched.
    [Test]
    public void CheckedInLedgerHasTheMeasuredCategoryCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<FoldInCandidateLedger.Row> rows = FoldInCandidateLedger.Read(root);

        int interfaceEdgeCaseOnly = rows.Count(r =>
            r.Layer == ObligationLayer.Interface && r.Category == FoldInCategory.EdgeCaseOnly
        );
        int interfaceNeverWitnessed = rows.Count(r =>
            r.Layer == ObligationLayer.Interface && r.Category == FoldInCategory.NeverWitnessed
        );
        int surfaceEdgeCaseOnly = rows.Count(r =>
            r.Layer == ObligationLayer.Surface && r.Category == FoldInCategory.EdgeCaseOnly
        );
        int surfacePresentElsewhere = rows.Count(r =>
            r.Layer == ObligationLayer.Surface && r.Category == FoldInCategory.PresentInLanguageGrammarAlready
        );

        TestContext.Out.WriteLine(
            $"rows={rows.Count} interfaceEdgeCaseOnly={interfaceEdgeCaseOnly} "
                + $"interfaceNeverWitnessed={interfaceNeverWitnessed} surfaceEdgeCaseOnly={surfaceEdgeCaseOnly} "
                + $"surfacePresentElsewhere={surfacePresentElsewhere}"
        );

        Assert.That(rows, Has.Count.EqualTo(91));
        Assert.That(interfaceEdgeCaseOnly, Is.EqualTo(1));
        Assert.That(interfaceNeverWitnessed, Is.EqualTo(25));
        Assert.That(surfaceEdgeCaseOnly, Is.EqualTo(57));
        Assert.That(surfacePresentElsewhere, Is.EqualTo(8));
    }

    // Was CompoundingRule.outputPartOfSpeech and MorphologicalInput.excludedMPRFeatures; now just the
    // former -- see CheckedInLedgerHasTheMeasuredCategoryCounts's own comment on why the latter dropped
    // out.
    [Test]
    public void InterfaceFoldInCandidatesAreTheEdgeCaseOnlyMprAndCompoundOutput()
    {
        string root = RepositoryRoot();
        IReadOnlyList<FoldInCandidateLedger.Row> rows = FoldInCandidateLedger.Read(root);

        FoldInCandidateLedger.Row[] candidates = rows.Where(r =>
                r.Layer == ObligationLayer.Interface && r.Category == FoldInCategory.EdgeCaseOnly
            )
            .ToArray();

        Assert.That(candidates, Has.Length.EqualTo(1));
        Assert.That(
            candidates.Select(r => r.Obligation),
            Is.EquivalentTo(new[] { "CompoundingRule.outputPartOfSpeech" })
        );
    }

    // Cheap: reads InterfaceInventoryLedger/InterfaceWitnessLedger/EvidenceLedger back off disk plus a
    // structural (no-reparse) GrammarFeatureUsage scan of every languages/* grammar -- no severance
    // sweep of its own, so no [Explicit] needed.
    [Test]
    public void CheckedInFoldInCandidateLedgerIsUpToDate()
    {
        string root = RepositoryRoot();
        IReadOnlyList<FoldInCandidateLedger.Row> fresh = FoldInCandidateLedger.Compute(root);
        string freshText = FoldInCandidateLedger.ToText(fresh);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, FoldInCandidateLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            freshText.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-coverage-traceability --repository-root ."
        );
    }
}
