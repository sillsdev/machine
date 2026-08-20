#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record RoslynCompilationNode(
    RepositoryGraphNodeKey Key,
    CSharpCompilation Compilation,
    CompilationDiagnostics Diagnostics,
    int ProbedSdkGeneratorCount
);

internal sealed class RoslynCompilationGraph
{
    private static readonly IReadOnlyDictionary<string, string> OwnedAssemblyNames = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["machine"] = "SIL.Machine",
        ["hc"] = "SIL.Machine.Morphology.HermitCrab",
        ["hc-tool"] = "SIL.Machine.Morphology.HermitCrab.Tool",
        ["hc-conformance"] = "hc-conformance",
    };

    private RoslynCompilationGraph(IReadOnlyDictionary<RepositoryGraphNodeKey, RoslynCompilationNode> nodes)
    {
        Nodes = nodes;
    }

    internal IReadOnlyDictionary<RepositoryGraphNodeKey, RoslynCompilationNode> Nodes { get; }

    internal RoslynCompilationNode this[RepositoryGraphNodeKey key] => Nodes[key];

    internal static RoslynCompilationGraph Build(RepositoryCompilationGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.CompilerInputs.Count != graph.Nodes.Count)
            throw new InvalidDataException("Compiler inputs must cover every compilation graph node.");

        var built = new Dictionary<RepositoryGraphNodeKey, RoslynCompilationNode>();
        var externalReferences = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (RepositoryProjectDefinition project in graph.Projects)
        {
            foreach (RepositoryGraphNode node in graph.Nodes.Where(item => item.ProjectId == project.Id))
            {
                CompilerInputModel input = graph.CompilerInputs[node.Key];
                var trees = input
                    .Sources.Select(source =>
                    {
                        using var stream = new MemoryStream(source.Content.ToArray(), writable: false);
                        SourceText text = SourceText.From(stream, encoding: null);
                        return CSharpSyntaxTree.ParseText(
                            text,
                            input.Arguments.ParseOptions as CSharpParseOptions,
                            source.Path
                        );
                    })
                    .ToImmutableArray();
                var diagnostics = CompilationDiagnostics.From(
                    node.Key.ToString(),
                    trees.SelectMany(tree => tree.GetDiagnostics())
                );
                diagnostics.ThrowIfFatal();

                IEnumerable<MetadataReference> references = CreateReferences(
                    graph,
                    node,
                    input,
                    built,
                    externalReferences
                );
                CSharpCompilation compilation = CSharpCompilation.Create(
                    ProjectAssemblyName(project.Id, input),
                    trees,
                    references,
                    input.Arguments.CompilationOptions as CSharpCompilationOptions
                        ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );
                CompilationDiagnostics compilerDiagnostics = CompilationDiagnostics.From(
                    node.Key.ToString(),
                    compilation.GetDiagnostics()
                );
                compilerDiagnostics.ThrowIfFatal();
                int probedSdkGeneratorCount = RunPendingGeneratorProbe(input, compilation);
                built.Add(
                    node.Key,
                    new RoslynCompilationNode(node.Key, compilation, compilerDiagnostics, probedSdkGeneratorCount)
                );
            }
        }

        return new RoslynCompilationGraph(built);
    }

    private static IEnumerable<MetadataReference> CreateReferences(
        RepositoryCompilationGraph graph,
        RepositoryGraphNode node,
        CompilerInputModel input,
        IReadOnlyDictionary<RepositoryGraphNodeKey, RoslynCompilationNode> built,
        IDictionary<string, PortableExecutableReference> externalReferences
    )
    {
        var directOwned = graph
            .ProjectEdges.Where(edge => edge.FromProjectId == node.ProjectId)
            .Select(edge => edge.ToProjectId)
            .ToHashSet(StringComparer.Ordinal);
        var owned = TransitiveOwnedProjects(graph, node.ProjectId);
        var ownedAssemblyNames = owned.ToDictionary(
            project => project,
            project =>
                ProjectAssemblyName(
                    project,
                    graph.CompilerInputs[
                        graph.Nodes.First(item => item.ProjectId == project && item.Profile.Id == node.Profile.Id).Key
                    ]
                ),
            StringComparer.Ordinal
        );
        var everyOwnedAssemblyName = graph.Projects.ToDictionary(
            project => project.Id,
            project =>
                ProjectAssemblyName(
                    project.Id,
                    graph.CompilerInputs[
                        graph
                            .Nodes.First(item => item.ProjectId == project.Id && item.Profile.Id == node.Profile.Id)
                            .Key
                    ]
                ),
            StringComparer.Ordinal
        );
        var consumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CommandLineReference reference in input.Arguments.MetadataReferences)
        {
            string? path = reference.Reference;
            string fileName = path is null ? string.Empty : Path.GetFileNameWithoutExtension(path);
            string? ownedProject = owned.FirstOrDefault(project =>
                ownedAssemblyNames.TryGetValue(project, out string? name)
                && string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase)
            );
            if (ownedProject is not null)
            {
                RepositoryGraphNodeKey upstreamKey = new(
                    ownedProject,
                    graph.Nodes.First(item => item.ProjectId == ownedProject).TargetFramework,
                    node.Profile.Id
                );
                if (!built.TryGetValue(upstreamKey, out RoslynCompilationNode? upstream))
                {
                    throw new InvalidDataException(
                        $"Owned project '{ownedProject}' has not been compiled before '{node.ProjectId}'."
                    );
                }
                consumed.Add(ownedProject);
                yield return upstream.Compilation.ToMetadataReference(
                    reference.Properties.Aliases,
                    reference.Properties.EmbedInteropTypes
                );
                continue;
            }

            if (
                everyOwnedAssemblyName.Values.Any(name =>
                    string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                throw new CompilerInputException(
                    "owned-binary-reference",
                    $"Owned reference '{path}' was not matched to an admitted direct project edge."
                );
            }

            if (path is null || string.IsNullOrWhiteSpace(path))
                throw new CompilerInputException("reference-parser-diagnostic", "A metadata reference has no path.");
            if (!File.Exists(path))
            {
                throw new CompilerInputException(
                    "reference-parser-diagnostic",
                    $"Metadata reference '{path}' does not exist."
                );
            }
            string externalKey =
                $"{Path.GetFullPath(path)}\0{reference.Properties.Kind}\0{reference.Properties.EmbedInteropTypes}\0{string.Join("\0", reference.Properties.Aliases)}";
            if (!externalReferences.TryGetValue(externalKey, out PortableExecutableReference? externalReference))
            {
                externalReference = LoadExternalReference(path, reference.Properties);
                externalReferences.Add(externalKey, externalReference);
            }
            yield return externalReference;
        }

        foreach (string project in directOwned.Where(project => !consumed.Contains(project)))
        {
            throw new CompilerInputException(
                "missing-owned-project-reference",
                $"Compiler inputs for '{node.ProjectId}' omit direct owned reference '{project}'."
            );
        }
    }

    internal static PortableExecutableReference LoadExternalReference(
        string path,
        MetadataReferenceProperties properties
    )
    {
        try
        {
            using (FileStream stream = File.OpenRead(path))
            using (var peReader = new PEReader(stream))
            {
                if (!peReader.HasMetadata)
                    throw new BadImageFormatException("The reference has no CLI metadata.");
                _ = peReader.GetMetadataReader();
            }
            PortableExecutableReference reference = MetadataReference.CreateFromFile(path, properties);
            return reference;
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or BadImageFormatException
                        or ArgumentException
            )
        {
            throw new CompilerInputException(
                "reference-parser-diagnostic",
                $"Metadata reference '{path}' could not be loaded.",
                exception
            );
        }
    }

    private static HashSet<string> TransitiveOwnedProjects(RepositoryCompilationGraph graph, string projectId)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(projectId);
        while (pending.Count != 0)
        {
            string current = pending.Pop();
            foreach (
                string dependency in graph
                    .ProjectEdges.Where(edge => edge.FromProjectId == current)
                    .Select(edge => edge.ToProjectId)
            )
            {
                if (result.Add(dependency))
                    pending.Push(dependency);
            }
        }
        return result;
    }

    private static int RunPendingGeneratorProbe(CompilerInputModel input, CSharpCompilation compilation)
    {
        AnalyzerMetadataInspection[] pending = input
            .Analyzers.Where(analyzer =>
                analyzer.Disposition == AnalyzerDisposition.SdkOwnedSourceGeneratorPendingProbe
            )
            .ToArray();
        if (pending.Length == 0)
            return 0;

        try
        {
            var generators = ImmutableArray.CreateBuilder<ISourceGenerator>();
            foreach (AnalyzerMetadataInspection analyzer in pending)
            {
                var reference = new AnalyzerFileReference(analyzer.Path, new GraphAnalyzerAssemblyLoader());
                ImmutableArray<ISourceGenerator> loaded = reference.GetGenerators(LanguageNames.CSharp);
                if (loaded.Length == 0)
                {
                    throw new CompilerInputException(
                        "source-generator-probe",
                        $"Admitted SDK generator assembly '{analyzer.Path}' exposed no C# generators."
                    );
                }
                generators.AddRange(loaded);
            }
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators.ToImmutable(),
                additionalTexts: input
                    .AdditionalFiles.Select(file => (AdditionalText)new CapturedAdditionalText(file))
                    .ToImmutableArray(),
                parseOptions: input.Arguments.ParseOptions as CSharpParseOptions,
                optionsProvider: new CapturedAnalyzerConfigOptionsProvider(input.AnalyzerConfigs)
            );
            driver = driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out Compilation updatedCompilation,
                out ImmutableArray<Diagnostic> diagnostics
            );
            if (updatedCompilation is not CSharpCompilation updated)
            {
                throw new CompilerInputException(
                    "source-generator-probe",
                    "The source-generator probe returned a non-C# compilation."
                );
            }
            GeneratorDriverRunResult runResult = driver.GetRunResult();
            bool generatorFailure =
                runResult.Diagnostics.Length != 0
                || runResult.Results.Any(result =>
                    result.Exception is not null
                    || result.Diagnostics.Length != 0
                    || result.GeneratedSources.Length != 0
                );
            if (
                updated.SyntaxTrees.Length != compilation.SyntaxTrees.Length
                || diagnostics.Length != 0
                || generatorFailure
            )
            {
                throw new CompilerInputException(
                    "source-generator-probe",
                    "An admitted SDK source generator produced output or diagnostics."
                );
            }
            return generators.Count;
        }
        catch (CompilerInputException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CompilerInputException(
                "source-generator-probe",
                "An admitted SDK source generator failed during the zero-output probe.",
                exception
            );
        }
    }

    private static string ProjectAssemblyName(string projectId, CompilerInputModel input)
    {
        if (input.Arguments.CompilationName is { Length: > 0 } commandLineName)
            return commandLineName;
        return input.Arguments.OutputFileName is { Length: > 0 } outputFile
                ? Path.GetFileNameWithoutExtension(outputFile)
            : OwnedAssemblyNames.TryGetValue(projectId, out string? name) ? name
            : projectId;
    }

    private sealed class GraphAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath) { }

        public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
    }

    private sealed class CapturedAdditionalText(CompilerAuxiliaryInput input) : AdditionalText
    {
        public override string Path { get; } = input.Path;

        public override SourceText? GetText(System.Threading.CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream(input.Content.ToArray(), writable: false);
            return SourceText.From(stream, encoding: null);
        }
    }

    private sealed class CapturedAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigSet _configs;
        private readonly AnalyzerConfigOptions _global;

        internal CapturedAnalyzerConfigOptionsProvider(IReadOnlyList<CompilerAuxiliaryInput> inputs)
        {
            AnalyzerConfig[] configs = inputs.Select(Parse).ToArray();
            _configs = AnalyzerConfigSet.Create(configs, out ImmutableArray<Diagnostic> diagnostics);
            ThrowIfConfigDiagnostics(diagnostics);
            AnalyzerConfigOptionsResult global = _configs.GlobalConfigOptions;
            ThrowIfConfigDiagnostics(global.Diagnostics);
            _global = new CapturedAnalyzerConfigOptions(global.AnalyzerOptions);
        }

        public override AnalyzerConfigOptions GlobalOptions => _global;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GetOptions(tree.FilePath);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GetOptions(textFile.Path);

        private AnalyzerConfigOptions GetOptions(string path)
        {
            AnalyzerConfigOptionsResult result = _configs.GetOptionsForSourcePath(path);
            ThrowIfConfigDiagnostics(result.Diagnostics);
            return new CapturedAnalyzerConfigOptions(result.AnalyzerOptions);
        }

        private static AnalyzerConfig Parse(CompilerAuxiliaryInput input)
        {
            using var stream = new MemoryStream(input.Content.ToArray(), writable: false);
            return AnalyzerConfig.Parse(SourceText.From(stream, encoding: null), input.Path);
        }

        private static void ThrowIfConfigDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.Length == 0)
                return;
            throw new CompilerInputException(
                "analyzer-config-diagnostic",
                string.Join("; ", diagnostics.Select(diagnostic => diagnostic.ToString()))
            );
        }
    }

    private sealed class CapturedAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        : AnalyzerConfigOptions
    {
        public override IEnumerable<string> Keys => values.Keys;

        public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
    }
}
