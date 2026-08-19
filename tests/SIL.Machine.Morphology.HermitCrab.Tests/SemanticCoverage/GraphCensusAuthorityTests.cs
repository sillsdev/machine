using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class GraphCensusAuthorityTests
{
    private static readonly string[] CensusedProjects = { "hc", "hc-tool" };

    private static readonly string[] AuditedScopes =
    {
        "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader.Load(System.String)",
    };

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

    private static async Task<(RepositoryCompilationGraph Captured, RoslynCompilationGraph Graph)> LoadAsync()
    {
        RepositoryCompilationGraph captured = await new RepositoryCompilationGraphLoader(new MsBuildProcessRunner())
            .LoadAsync(new RepositoryRoot(RepositoryRoot()), CancellationToken.None);
        return (captured, RoslynCompilationGraph.Build(captured));
    }

    [Test]
    [CancelAfter(300_000)]
    public async Task LiveGraphCensusResolvesEveryScopeWithoutApproximation()
    {
        (RepositoryCompilationGraph captured, RoslynCompilationGraph graph) = await LoadAsync();

        SemanticInventory inventory = CSharpInventoryReader.ReadFromGraph(
            graph, captured, RepositoryRoot(), CensusedProjects, AuditedScopes);

        Assert.Multiple(() =>
        {
            Assert.That(inventory.Surfaces, Is.Not.Empty, "the graph census must produce surfaces");
            Assert.That(
                inventory.Diagnostics.Select(item => item.Code),
                Has.None.EqualTo("compilation-error"),
                string.Join(
                    Environment.NewLine,
                    inventory.Diagnostics.Where(item => item.Code == "compilation-error")
                        .Select(item => $"{item.Location}: {item.Message}")));
            Assert.That(inventory.SourceHash, Is.Not.Empty);
        });
    }

    // The census hash must move when the compiler inputs move, or a stale product cannot be detected.
    [Test]
    [CancelAfter(300_000)]
    public async Task LiveGraphCensusIsBoundToTheGraphHash()
    {
        (RepositoryCompilationGraph captured, RoslynCompilationGraph graph) = await LoadAsync();

        SemanticInventory first = CSharpInventoryReader.ReadFromGraph(
            graph, captured, RepositoryRoot(), CensusedProjects, AuditedScopes);
        SemanticInventory second = CSharpInventoryReader.ReadFromGraph(
            graph, captured, RepositoryRoot(), CensusedProjects, AuditedScopes);

        Assert.That(second.SourceHash, Is.EqualTo(first.SourceHash));
    }

    [Test]
    [CancelAfter(300_000)]
    public async Task GraphCensusRefusesAnUnknownProjectAndEmptyScopes()
    {
        (RepositoryCompilationGraph captured, RoslynCompilationGraph graph) = await LoadAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CSharpInventoryReader.ReadFromGraph(
                    graph, captured, RepositoryRoot(), new[] { "not-a-project" }, AuditedScopes),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => CSharpInventoryReader.ReadFromGraph(
                    graph, captured, RepositoryRoot(), CensusedProjects, Array.Empty<string>()),
                Throws.TypeOf<ArgumentException>());
        });
    }
}
