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

        Assert.That(rows, Has.Count.EqualTo(752));
        Assert.That(distinctFixtures, Is.EqualTo(44));
        Assert.That(surface, Is.EqualTo(189));
        Assert.That(@interface, Is.EqualTo(462));
        Assert.That(construct, Is.EqualTo(101));
    }

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
