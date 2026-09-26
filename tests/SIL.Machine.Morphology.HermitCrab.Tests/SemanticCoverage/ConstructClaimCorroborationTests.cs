using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConstructClaimCorroborationTests
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
    public void CheckedInLedgerHasTheMeasuredClaimAndStatusCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<ConstructClaimCorroboration.Row> rows = ConstructClaimCorroboration.Read(root);

        int confirmed = rows.Count(r => r.Status == ConstructClaimStatus.Confirmed);
        int contradicted = rows.Count(r => r.Status == ConstructClaimStatus.Contradicted);
        int unmapped = rows.Count(r => r.Status == ConstructClaimStatus.Unmapped);

        TestContext.Out.WriteLine(
            $"rows={rows.Count} confirmed={confirmed} contradicted={contradicted} unmapped={unmapped}"
        );

        Assert.That(rows, Has.Count.EqualTo(506));
        Assert.That(confirmed, Is.EqualTo(216));
        Assert.That(contradicted, Is.EqualTo(12));
        Assert.That(unmapped, Is.EqualTo(278));
    }

    [Test]
    public void AllContradictedClaimsTraceToTheKnownBundledConstructLimitation()
    {
        string root = RepositoryRoot();
        IReadOnlyList<ConstructClaimCorroboration.Row> rows = ConstructClaimCorroboration.Read(root);
        ConstructClaimCorroboration.Row[] contradicted = rows.Where(r => r.Status == ConstructClaimStatus.Contradicted)
            .ToArray();

        Assert.That(contradicted, Has.Length.EqualTo(12));
        Assert.That(
            contradicted.Select(r => r.Fixture).Distinct(),
            Is.EquivalentTo(new[] { "edge-cases/morphotactic-attribute-breadth" })
        );
        Assert.That(
            contradicted.Select(r => r.Construct).Distinct(),
            Is.EquivalentTo(
                new[] { "Ordinary/realizational rule constraints (MaxApplicationCount/RequiredStemName/Blockable)" }
            )
        );
        Assert.That(contradicted.Select(r => r.MatchedTokens.Count), Is.All.EqualTo(1));
    }

    // Cheap: constructs.txt, the DTD (once), and every fixture's already-loaded grammar.xml/words.yaml
    // -- no reparse, so this runs on every pass rather than needing [Explicit].
    [Test]
    public void CheckedInConstructClaimCorroborationIsUpToDate()
    {
        string root = RepositoryRoot();
        string constructsPath = Path.Combine(root, "conformance", "constructs.txt");
        IReadOnlyList<ConstructClaimCorroboration.Row> fresh = ConstructClaimCorroboration.Compute(
            root,
            constructsPath
        );
        string freshText = ConstructClaimCorroboration.ToText(fresh);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, ConstructClaimCorroboration.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            freshText.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-coverage-traceability --repository-root ."
        );
    }
}
