using System.Diagnostics;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class CoverageCliAuthorityTests
{
    private string _repositoryRoot = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _repositoryRoot = Path.Combine(Path.GetTempPath(), "hc-coverage-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_repositoryRoot, "conformance"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_repositoryRoot))
            Directory.Delete(_repositoryRoot, recursive: true);
    }

    [Test]
    public void CoverageEvidenceExitsNonzeroWhenTheAuthoritativeGateRejectsAProof()
    {
        CounterfactualLedger.Write(
            _repositoryRoot,
            new[]
            {
                new CounterfactualResult(
                    "dtd:enum/Stratum/cyclicity/cyclic",
                    "edge-cases/synthetic",
                    CounterfactualVerdict.Unobservable,
                    "set cyclicity",
                    "unchanged"
                ),
            }
        );
        File.WriteAllText(
            Path.Combine(_repositoryRoot, ImpossibilityProofs.RelativePath),
            "surface\tkind\tevidence\n"
                + "dtd:enum/Stratum/cyclicity/cyclic\tno-consumer\tchecked-in prose is not a proof\n"
        );

        ProcessResult result = RunCoverageCli("--coverage-evidence");

        Assert.That(result.ExitCode, Is.EqualTo(1), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Contain("rejected proof"));
        Assert.That(result.CombinedOutput, Does.Contain("complete: False"));
    }

    // The checked-in catalog's empty auditedSourceScopes no longer short-circuits the run: the
    // semantic coverage program audits the grammar format (dtd: surfaces), not the C# engine's
    // internals, so an empty scope list runs the audit against the DTD-only inventory and fails
    // on the real, expected-for-now backlog (unclassified grammar features) instead.
    [Test]
    [CancelAfter(300_000)]
    public void CheckedInEmptyCatalogRunsTheAuditAndFailsOnUnclassifiedMappings()
    {
        string root = RepositoryRoot();
        ProcessResult result = RunCoverageCli("--semantic-coverage", root);

        Assert.That(result.ExitCode, Is.EqualTo(1), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Not.Contain("audited-source-scopes-empty"));
        Assert.That(result.CombinedOutput, Does.Contain("generated surfaces:"));
        Assert.That(result.CombinedOutput, Does.Contain("unclassified-mapping"));
    }

    [Test]
    public void ProposalNeverChangesCanonicalCatalogBytes()
    {
        string root = RepositoryRoot();
        string catalogPath = Path.Combine(root, "conformance", "semantic-catalog.yaml");
        byte[] before = File.ReadAllBytes(catalogPath);

        ProcessResult result = RunCoverageCli(
            "--propose-semantic-catalog",
            root,
            "--audited-source-scope",
            "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader");

        Assert.That(result.ExitCode, Is.EqualTo(1), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Contain("proposal only"));
        Assert.That(result.CombinedOutput, Does.Contain("auditedSourceScopes: [\"SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader\"]"));
        Assert.That(File.ReadAllBytes(catalogPath), Is.EqualTo(before));
    }

    [Test]
    public void EmptyCatalogWithoutExplicitProposalScopeIsControlledError()
    {
        string root = RepositoryRoot();
        ProcessResult result = RunCoverageCli("--propose-semantic-catalog", root);

        Assert.That(result.ExitCode, Is.EqualTo(2), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Contain("requires one or more"));
    }

    // The census is the compiler's own graph now, so a CLI census test needs a restored
    // repository; a synthetic directory has no compiler inputs to capture. The checked-in
    // catalog carries no audited scopes, so --semantic-coverage's own census contributes no C#
    // surfaces; the proposal with an explicit scope is the only way to exercise the live C#
    // census from the CLI.
    [Test]
    [CancelAfter(300_000)]
    public void ScopedProposalRunsTheLiveGraphCensusAndPreservesCatalog()
    {
        string root = RepositoryRoot();
        string catalogPath = Path.Combine(root, "conformance", "semantic-catalog.yaml");
        byte[] before = File.ReadAllBytes(catalogPath);

        ProcessResult result = RunCoverageCli(
            "--propose-semantic-catalog",
            root,
            "--audited-source-scope",
            "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader.Load(System.String)");

        Assert.That(result.ExitCode, Is.EqualTo(1), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Contain("schema/decision-if"), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Not.Contain("proposal unavailable"), result.CombinedOutput);
        Assert.That(File.ReadAllBytes(catalogPath), Is.EqualTo(before));
    }

    [Test]
    public void WildcardProposalScopeIsControlledError()
    {
        string root = CreateSyntheticRepository();
        ProcessResult result = RunCoverageCli(
            "--propose-semantic-catalog",
            root,
            "--audited-source-scope",
            "Fixture.*");

        Assert.That(result.ExitCode, Is.EqualTo(2), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Contain("patterns are not allowed"));
    }

    [Test]
    public void MissingRequiredBaselineIsControlledAuthorityError()
    {
        string root = CreateSyntheticRepository();
        File.Delete(Path.Combine(root, "conformance", "semantic-coverage-baseline.txt"));

        ProcessResult result = RunCoverageCli("--semantic-coverage", root);

        Assert.That(result.ExitCode, Is.EqualTo(2), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Contain("semantic coverage authority unavailable"));
    }

    [Test]
    public void MalformedFixtureGrammarIsControlledAuthorityError()
    {
        string root = CreateSyntheticRepository();
        string fixture = Path.Combine(root, "conformance", "malformed");
        Directory.CreateDirectory(fixture);
        File.WriteAllText(Path.Combine(fixture, "grammar.xml"), "<not-closed>");

        ProcessResult result = RunCoverageCli("--semantic-coverage", root);

        Assert.That(result.ExitCode, Is.EqualTo(2), result.CombinedOutput);
        Assert.That(result.CombinedOutput, Does.Contain("semantic coverage authority unavailable"));
        Assert.That(result.CombinedOutput, Does.Not.Contain("Unhandled exception"));
    }

    private ProcessResult RunCoverageCli(string mode, string? repositoryRoot = null, params string[] extraArgs)
    {
        string tool = Path.Combine(TestContext.CurrentContext.TestDirectory, "hc-conformance.dll");
        Assert.That(File.Exists(tool), Is.True, $"missing test-side CLI at {tool}");
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(tool);
        start.ArgumentList.Add(mode);
        start.ArgumentList.Add("--repository-root");
        start.ArgumentList.Add(repositoryRoot ?? _repositoryRoot);
        foreach (string extraArg in extraArgs)
            start.ArgumentList.Add(extraArg);

        using Process process = Process.Start(start)!;
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout + stderr);
    }

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

    private string CreateSyntheticRepository()
    {
        string engine = Path.Combine(_repositoryRoot, "src", "SIL.Machine.Morphology.HermitCrab");
        string tool = Path.Combine(_repositoryRoot, "src", "SIL.Machine.Morphology.HermitCrab.Tool");
        Directory.CreateDirectory(engine);
        Directory.CreateDirectory(tool);
        File.WriteAllText(Path.Combine(_repositoryRoot, "conformance", "constructs.txt"), "synthetic\n");
        File.WriteAllText(Path.Combine(engine, "HermitCrabInput.dtd"), "<!ELEMENT Root EMPTY>\n");
        File.WriteAllText(Path.Combine(engine, "Engine.cs"),
            "namespace Fixture { public sealed class Root { public void Run(bool value) { if (value) { } } } }");
        File.WriteAllText(Path.Combine(tool, "Tool.cs"), "namespace Fixture { public sealed class Tool { } }");
        File.WriteAllText(Path.Combine(_repositoryRoot, "conformance", "semantic-coverage-baseline.txt"), "");
        File.WriteAllText(Path.Combine(_repositoryRoot, "conformance", "semantic-coverage-presence-waivers.txt"), "");
        File.WriteAllText(Path.Combine(_repositoryRoot, "conformance", "semantic-catalog.yaml"),
            "profile: sil.machine.hc-semantic-catalog/v1\n"
            + "auditedSourceScopes: [Fixture.Root]\n"
            + "features: []\n"
            + "surfaceMappings: []\n");
        return _repositoryRoot;
    }

    private sealed record ProcessResult(int ExitCode, string CombinedOutput);
}
