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
