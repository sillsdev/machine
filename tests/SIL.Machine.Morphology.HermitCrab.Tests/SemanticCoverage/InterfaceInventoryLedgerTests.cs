using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class InterfaceInventoryLedgerTests
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
    public void RealCorpusProducesTheDeclaredPresentEdgeAndJunctionCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceInventoryLedger.Row> rows = InterfaceInventoryLedger.Compute(root);

        int present = rows.Count(r => r.Present);
        int typedEdges = rows.Sum(r => r.ObservedTargetTypes.Count);
        IReadOnlyList<InterfaceJunction> junctions = InterfaceInventoryLedger.ComputeJunctions(rows);

        TestContext.Out.WriteLine($"declared={rows.Count} present={present} notPresent={rows.Count - present}");
        TestContext.Out.WriteLine($"typedEdges={typedEdges}");
        foreach (InterfaceJunction junction in junctions)
        {
            TestContext.Out.WriteLine(
                $"junction: {junction.TargetType} writers={junction.WriterCount} readers={junction.ReaderCount}"
            );
        }

        Assert.That(rows, Has.Count.EqualTo(60));
        Assert.That(present, Is.EqualTo(44));
        Assert.That(rows.Count - present, Is.EqualTo(16));
        Assert.That(typedEdges, Is.EqualTo(51));
        Assert.That(junctions, Has.Count.EqualTo(3));
        Assert.That(
            junctions.Select(j => j.TargetType),
            Is.EquivalentTo(new[] { "MorphologicalPhonologicalRuleFeature", "PartOfSpeech", "StemName" })
        );

        InterfaceJunction mprFeature = junctions.Single(j => j.TargetType == "MorphologicalPhonologicalRuleFeature");
        Assert.That(mprFeature.WriterCount, Is.EqualTo(2));
        Assert.That(mprFeature.ReaderCount, Is.EqualTo(7));

        InterfaceJunction partOfSpeech = junctions.Single(j => j.TargetType == "PartOfSpeech");
        Assert.That(partOfSpeech.WriterCount, Is.EqualTo(3));
        Assert.That(partOfSpeech.ReaderCount, Is.EqualTo(5));

        InterfaceJunction stemName = junctions.Single(j => j.TargetType == "StemName");
        Assert.That(stemName.WriterCount, Is.EqualTo(1));
        Assert.That(stemName.ReaderCount, Is.EqualTo(1));
    }

    // Every present row must name at least one fixture, and every not-present row must name none --
    // this pins that the two fields (Present, Fixtures) can never disagree with each other.
    [Test]
    public void PresentRowsNameFixturesAndOnlyPresentRowsDo()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceInventoryLedger.Row> rows = InterfaceInventoryLedger.Compute(root);

        Assert.Multiple(() =>
        {
            foreach (InterfaceInventoryLedger.Row row in rows)
            {
                if (row.Present)
                {
                    Assert.That(
                        row.Fixtures,
                        Is.Not.Empty,
                        $"{row.Element}.{row.Attribute} is present but names no fixture"
                    );
                }
                else
                {
                    Assert.That(
                        row.Fixtures,
                        Is.Empty,
                        $"{row.Element}.{row.Attribute} is not present but names a fixture"
                    );
                }
            }
        });
    }

    [Test]
    public void CheckedInInterfaceInventoryLedgerIsUpToDate()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceInventoryLedger.Row> rows = InterfaceInventoryLedger.Compute(root);

        string fresh = InterfaceInventoryLedger.ToText(rows);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, InterfaceInventoryLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            fresh.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-interface-inventory --repository-root ."
        );
    }
}
