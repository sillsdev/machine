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
    // declared across HermitCrabInput.dtd, 42 of them are PRESENT in at least one of the 33 fixtures
    // (18 are not), the 42 present ones resolve to 51 distinct (element, attribute, target-type)
    // edges, and exactly three target types -- MorphologicalPhonologicalRuleFeature, PartOfSpeech,
    // and StemName -- are reached by both a write-direction and a read-direction edge, per
    // SemanticInterfaceDirection's engine-checked judgment (NOT a name-prefix guess: that heuristic
    // missed StemName entirely -- Allomorph.stemName matches neither its write nor read prefixes --
    // and undercounted the other two, since it also misclassifies LexicalEntry.partOfSpeech and
    // LexicalEntry.ruleFeatures as Ref rather than Write). Unlike RuleInteractionLedger's
    // corpus-statistic denominator, the "60 declared" figure here cannot move by adding a fixture;
    // only "present" and "typed edges" can. "Present" is STRUCTURAL ONLY (see Row's own doc comment)
    // -- InterfaceWitnessLedgerTests pins the separate, strictly weaker WITNESSED counts.
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
            TestContext.Out.WriteLine($"junction: {junction.TargetType} writers={junction.WriterCount} readers={junction.ReaderCount}");

        Assert.That(rows, Has.Count.EqualTo(60));
        Assert.That(present, Is.EqualTo(44));
        Assert.That(rows.Count - present, Is.EqualTo(16));
        Assert.That(typedEdges, Is.EqualTo(53));
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
                    Assert.That(row.Fixtures, Is.Not.Empty, $"{row.Element}.{row.Attribute} is present but names no fixture");
                else
                    Assert.That(row.Fixtures, Is.Empty, $"{row.Element}.{row.Attribute} is not present but names a fixture");
            }
        });
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
