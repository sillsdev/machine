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

    // Pins the four headline counts from the DTD+corpus derivation: 60 IDREF/IDREFS attributes are
    // declared across HermitCrabInput.dtd, 42 of them are exercised by at least one of the 33
    // fixtures (18 are not), the 42 exercised ones resolve to 51 distinct (element, attribute,
    // target-type) edges, and exactly two target types -- MorphologicalPhonologicalRuleFeature and
    // PartOfSpeech -- are reached by both a write-direction and a read-direction edge. Unlike
    // RuleInteractionLedger's corpus-statistic denominator, the "60 declared" figure here cannot move
    // by adding a fixture; only "exercised" and "typed edges" can.
    [Test]
    public void RealCorpusProducesTheDeclaredExercisedEdgeAndJunctionCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceInventoryLedger.Row> rows = InterfaceInventoryLedger.Compute(root);

        int exercised = rows.Count(r => r.Exercised);
        int typedEdges = rows.Sum(r => r.ObservedTargetTypes.Count);
        IReadOnlyList<InterfaceJunction> junctions = InterfaceInventoryLedger.ComputeJunctions(rows);

        TestContext.Out.WriteLine($"declared={rows.Count} exercised={exercised} unexercised={rows.Count - exercised}");
        TestContext.Out.WriteLine($"typedEdges={typedEdges}");
        foreach (InterfaceJunction junction in junctions)
            TestContext.Out.WriteLine($"junction: {junction.TargetType} writers={junction.WriterCount} readers={junction.ReaderCount}");

        Assert.That(rows, Has.Count.EqualTo(60));
        Assert.That(exercised, Is.EqualTo(42));
        Assert.That(rows.Count - exercised, Is.EqualTo(18));
        Assert.That(typedEdges, Is.EqualTo(51));
        Assert.That(junctions, Has.Count.EqualTo(2));
        Assert.That(junctions.Select(j => j.TargetType), Is.EquivalentTo(new[] { "MorphologicalPhonologicalRuleFeature", "PartOfSpeech" }));

        InterfaceJunction mprFeature = junctions.Single(j => j.TargetType == "MorphologicalPhonologicalRuleFeature");
        Assert.That(mprFeature.WriterCount, Is.EqualTo(1));
        Assert.That(mprFeature.ReaderCount, Is.EqualTo(5));

        InterfaceJunction partOfSpeech = junctions.Single(j => j.TargetType == "PartOfSpeech");
        Assert.That(partOfSpeech.WriterCount, Is.EqualTo(2));
        Assert.That(partOfSpeech.ReaderCount, Is.EqualTo(5));
    }

    // Mirrors OrderingGeneratorTests.CheckedInRuleInteractionLedgerIsUpToDate: regenerate the ledger
    // from the DTD plus the real corpus and require the checked-in file to match byte for byte, so a
    // DTD edit or a fixture change that shifts either half of the derivation is caught as a diff
    // instead of silently going stale.
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
