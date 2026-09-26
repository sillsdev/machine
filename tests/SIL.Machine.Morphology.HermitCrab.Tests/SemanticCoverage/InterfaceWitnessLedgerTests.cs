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
        int distinctInterfacesEverEvidenced = rows.Where(r => r.Verdict == CounterfactualVerdict.Evidenced)
            .Select(r => (r.Element, r.Attribute))
            .Distinct()
            .Count();

        TestContext.Out.WriteLine(
            $"rows={rows.Count} evidenced={evidenced} requiredByDtd={requiredByDtd} "
                + $"requiredByLoader={requiredByLoader} timeout={timeout} unobservable={unobservable} "
                + $"distinctInterfacesEvidenced={distinctInterfacesEverEvidenced}"
        );

        Assert.That(rows, Has.Count.EqualTo(462));
        Assert.That(evidenced, Is.EqualTo(104));
        Assert.That(requiredByDtd, Is.EqualTo(216));
        Assert.That(requiredByLoader, Is.EqualTo(31));
        Assert.That(timeout, Is.EqualTo(0));
        Assert.That(unobservable, Is.EqualTo(111));
        Assert.That(distinctInterfacesEverEvidenced, Is.EqualTo(19));
    }

    [Test]
    public void DefaultSymbolIsPresentButNeverWitnessed()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InterfaceWitnessResult> rows = InterfaceWitnessLedger.Read(root);

        InterfaceWitnessResult row = rows.Single(r =>
            r.Element == "SymbolicFeature"
            && r.Attribute == "defaultSymbol"
            && r.FixtureId == "edge-cases/loader-default-symbol"
        );

        Assert.That(row.Verdict, Is.EqualTo(CounterfactualVerdict.Unobservable));
    }

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
