using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class RoslynCompilationGraphTests
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
    public void CompilationDiagnosticsRetainWarningsButErrorsAreFatal()
    {
        MetadataReference[] references = { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        CSharpCompilation warningCompilation = CSharpCompilation.Create(
            "Fixture",
            new[] { CSharpSyntaxTree.ParseText("class C { void M() { int unused = 1; } }") },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        CSharpCompilation promotedWarningCompilation = warningCompilation.WithOptions(
            warningCompilation.Options.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        );
        CSharpCompilation errorCompilation = CSharpCompilation.Create(
            "BrokenFixture",
            new[] { CSharpSyntaxTree.ParseText("class C { MissingType Value; }") },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        CompilationDiagnostics warnings = CompilationDiagnostics.From("fixture", warningCompilation.GetDiagnostics());
        CompilationDiagnostics promoted = CompilationDiagnostics.From(
            "promoted",
            promotedWarningCompilation.GetDiagnostics()
        );
        CompilationDiagnostics errors = CompilationDiagnostics.From("broken", errorCompilation.GetDiagnostics());

        Assert.Multiple(() =>
        {
            Assert.That(warnings.Warnings.Select(item => item.Code), Does.Contain("CS0219"));
            Assert.That(warnings.Errors, Is.Empty);
            Assert.That(warnings.ThrowIfFatal, Throws.Nothing);
            Assert.That(promoted.Errors.Select(item => item.Code), Does.Contain("CS0219"));
            Assert.That(
                promoted.ThrowIfFatal,
                Throws
                    .TypeOf<CompilerInputException>()
                    .With.Property(nameof(CompilerInputException.Code))
                    .EqualTo("compiler-error")
            );
            Assert.That(errors.Errors.Select(item => item.Code), Does.Contain("CS0246"));
            Assert.That(
                errors.ThrowIfFatal,
                Throws
                    .TypeOf<CompilerInputException>()
                    .With.Property(nameof(CompilerInputException.Code))
                    .EqualTo("compiler-error")
            );
        });
    }

    [Test]
    public void MalformedExternalMetadataIsAControlledFatalDiagnostic()
    {
        string malformed = Path.Combine(RepositoryRoot(), "conformance", "constructs.txt");

        CompilerInputException exception = Assert.Throws<CompilerInputException>(() =>
            RoslynCompilationGraph.LoadExternalReference(malformed, MetadataReferenceProperties.Assembly)
        )!;

        Assert.That(exception.Code, Is.EqualTo("reference-parser-diagnostic"));
    }

    [Test]
    [CancelAfter(120_000)]
    public async Task LiveGraphBuildsAllNodesWithOwnedCompilationReferencesAndNoErrors()
    {
        string root = RepositoryRoot();
        RepositoryCompilationGraph captured = await new RepositoryCompilationGraphLoader(
            new MsBuildProcessRunner()
        ).LoadAsync(new RepositoryRoot(root), CancellationToken.None);

        RoslynCompilationGraph graph = RoslynCompilationGraph.Build(captured);

        Assert.Multiple(() =>
        {
            Assert.That(graph.Nodes, Has.Count.EqualTo(16));
            Assert.That(graph.Nodes.Values.All(node => node.Diagnostics.Errors.Count == 0), Is.True);
            Assert.That(
                graph
                    .Nodes.Values.Where(node => node.Key.TargetFramework == "net10.0")
                    .All(node => node.ProbedSdkGeneratorCount > 0),
                Is.True
            );
            Assert.That(
                graph
                    .Nodes.Values.Where(node => node.Key.TargetFramework == "netstandard2.0")
                    .All(node => node.ProbedSdkGeneratorCount == 0),
                Is.True
            );
            Assert.That(
                graph
                    .Nodes.Values.Where(node => node.Key.ProjectId != "machine")
                    .All(node => node.Compilation.References.Any(reference => reference is CompilationReference)),
                Is.True
            );
            Assert.That(
                graph
                    .Nodes.Values.SelectMany(node => node.Compilation.References)
                    .OfType<PortableExecutableReference>()
                    .Any(reference => reference.FilePath is not null && IsOwnedOutputPath(reference.FilePath)),
                Is.False
            );
        });
    }

    private static bool IsOwnedOutputPath(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        return name is ("SIL.Machine" or "SIL.Machine.Morphology.HermitCrab" or "hc" or "hc-conformance")
            && path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part =>
                    part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
                );
    }
}
