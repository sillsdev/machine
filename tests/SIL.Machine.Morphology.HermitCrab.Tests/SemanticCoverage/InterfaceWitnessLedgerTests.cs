using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class InterfaceWitnessLedgerTests
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

    // Pins the headline this whole split exists to produce: of the 42 interfaces
    // InterfaceInventoryLedger marks present, only 16 ever reach a word-level Evidenced verdict
    // ANYWHERE in the corpus -- the other 26 are present-but-never-witnessed (see
    // FoldInCandidateLedgerTests for that split by category). Presence overclaimed by nearly 3x.
    // timeout is pinned at 0 deliberately. Four rows here once recorded Timeout and were argued to be
    // genuinely slow rather than load-dependent; they later resolved on a quieter machine, two of them
    // to Evidenced. A Timeout is a statement about the machine, not the grammar, so any reappearance
    // is a signal to re-sweep rather than a verdict to record.
    [Test]
    public void CheckedInLedgerHasTheMeasuredVerdictAndWitnessedInterfaceCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceWitnessResult> rows = InterfaceWitnessLedger.Read(root);

        int evidenced = rows.Count(r => r.Verdict == CounterfactualVerdict.Evidenced);
        int requiredByDtd = rows.Count(r => r.Verdict == CounterfactualVerdict.RequiredByDtd);
        int requiredByLoader = rows.Count(r => r.Verdict == CounterfactualVerdict.RequiredByLoader);
        int timeout = rows.Count(r => r.Verdict == CounterfactualVerdict.Timeout);
        int unobservable = rows.Count(r => r.Verdict == CounterfactualVerdict.Unobservable);
        int distinctInterfacesEverEvidenced = rows
            .Where(r => r.Verdict == CounterfactualVerdict.Evidenced)
            .Select(r => (r.Element, r.Attribute))
            .Distinct()
            .Count();

        TestContext.Out.WriteLine(
            $"rows={rows.Count} evidenced={evidenced} requiredByDtd={requiredByDtd} "
                + $"requiredByLoader={requiredByLoader} timeout={timeout} unobservable={unobservable} "
                + $"distinctInterfacesEvidenced={distinctInterfacesEverEvidenced}"
        );

        Assert.That(rows, Has.Count.EqualTo(362));
        Assert.That(evidenced, Is.EqualTo(76));
        Assert.That(requiredByDtd, Is.EqualTo(170));
        Assert.That(requiredByLoader, Is.EqualTo(24));
        Assert.That(timeout, Is.EqualTo(0));
        Assert.That(unobservable, Is.EqualTo(92));
        Assert.That(distinctInterfacesEverEvidenced, Is.EqualTo(16));
    }

    // A concrete instance of the overclaim this ledger exists to catch: InterfaceInventoryLedger
    // marks SymbolicFeature.defaultSymbol present (it appears, with a real IDREF value, in
    // edge-cases/loader-default-symbol), yet severing it there changes neither of that fixture's two
    // words. Presence said "exercised"; witness says "inert". If this ever flips, that is real news
    // about the grammar or the fixture, not a reason to update this pin without checking why.
    [Test]
    public void DefaultSymbolIsPresentButNeverWitnessed()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceWitnessResult> rows = InterfaceWitnessLedger.Read(root);

        InterfaceWitnessResult row = rows.Single(
            r => r.Element == "SymbolicFeature" && r.Attribute == "defaultSymbol" && r.FixtureId == "edge-cases/loader-default-symbol"
        );

        Assert.That(row.Verdict, Is.EqualTo(CounterfactualVerdict.Unobservable));
    }

    // Mirrors InterfaceInventoryLedgerTests.CheckedInInterfaceInventoryLedgerIsUpToDate and
    // EvidenceLedgerFreshnessTests: recompute the whole severance sweep and require the checked-in
    // file to match byte for byte. [Explicit] because -- unlike InterfaceInventoryLedger's cheap,
    // reparse-free Compute() -- this re-parses every present interface x fixture pair (362 severance
    // runs plus one baseline per contributing fixture), the same cost class as
    // EvidenceLedgerFreshnessTests' own full sweep. Measured locally: ~2m45s.
    [Test]
    [Explicit("re-parses every present interface x fixture pair; ~2-3 minutes")]
    [Category("Counterfactual")]
    public void TheCheckedInWitnessLedgerMatchesAFreshRecomputeExactly()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceWitnessResult> fresh = InterfaceWitnessLedger.Sweep(root);

        string freshText = InterfaceWitnessLedger.ToText(fresh);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, InterfaceWitnessLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            freshText.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-coverage-traceability --repository-root ."
        );
    }
}
