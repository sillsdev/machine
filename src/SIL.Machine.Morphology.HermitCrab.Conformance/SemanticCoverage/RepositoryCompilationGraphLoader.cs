#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record CompilationGraphHashEnvironment(
    RepositoryCompilationGraph Graph,
    IReadOnlyDictionary<RepositoryGraphNodeKey, CapturedCompilerInputs> Captures,
    IReadOnlyDictionary<RepositoryGraphNodeKey, CompilerInputModel> CompilerInputs,
    string RepositoryRoot,
    string PrivateRoot,
    string CaptureTarget);

internal sealed class RepositoryCompilationGraphLoader
{
    internal static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(120);
    internal const int MaximumStandardOutputBytes = 64 * 1024 * 1024;

    private const string CaptureProperties =
        "PanGlossCompilerInputProtocol,MSBuildAllProjects,AssemblyName,TargetFramework,LangVersion,Nullable," +
        "DefineConstants,AllowUnsafeBlocks,CheckForOverflowUnderflow,OutputType,NETCoreSdkVersion,MSBuildVersion,CscToolPath,RoslynAssembliesPath," +
        "GeneratedAssemblyInfoFile,TargetFrameworkMonikerAssemblyAttributesPath";
    private const string CaptureItems =
        "CscCommandLineArgs,Compile,ProjectReference,ReferencePathWithRefAssemblies,Analyzer,AdditionalFiles,EditorConfigFiles,Using";

    private readonly IMsBuildProcessRunner _runner;
    private readonly Func<string> _privateDirectoryFactory;
    private readonly Func<CompilationGraphHashEnvironment, CompilationGraphHashInputs>? _hashInputBuilder;

    internal RepositoryCompilationGraphLoader(
        IMsBuildProcessRunner runner,
        Func<string>? privateDirectoryFactory = null,
        Func<CompilationGraphHashEnvironment, CompilationGraphHashInputs>? hashInputBuilder = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _hashInputBuilder = hashInputBuilder;
        _privateDirectoryFactory = privateDirectoryFactory ?? (() => Path.Combine(
            Path.GetTempPath(),
            "hc-semantic-msbuild",
            Guid.NewGuid().ToString("N")));
    }

    public async ValueTask<RepositoryCompilationGraph> LoadAsync(
        RepositoryRoot root,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(root);
        ValidateRepositoryRoot(root.FullPath);
        RepositoryCompilationGraph graph = RepositoryCompilationGraph.CreateFixed();
        string captureTarget = ResolveRepositoryFile(root.FullPath, "eng/HcSemanticCompilerInputs.targets");
        string privateRoot = Path.GetFullPath(_privateDirectoryFactory());
        if (Directory.Exists(privateRoot) || File.Exists(privateRoot))
            throw new InvalidDataException("The private MSBuild query directory must not already exist.");
        Directory.CreateDirectory(privateRoot);

        try
        {
            var baseDefines = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (RepositoryProjectDefinition project in graph.Projects)
            {
                ProjectPaths paths = ValidateProjectInputs(root.FullPath, project);
                ProcessCapture capture = await RunCheckedAsync(
                    CreateBasePropertyQuery(root.FullPath, project, paths),
                    cancellationToken).ConfigureAwait(false);
                baseDefines.Add(project.Id, ParseBaseDefines(capture.StandardOutput, project.TargetFramework));
            }

            var captures = new Dictionary<RepositoryGraphNodeKey, CapturedCompilerInputs>();
            var compilerInputs = new Dictionary<RepositoryGraphNodeKey, CompilerInputModel>();
            foreach (RepositoryGraphNode node in graph.Nodes)
            {
                RepositoryProjectDefinition project = graph.Projects.Single(item => item.Id == node.ProjectId);
                ProjectPaths paths = ValidateProjectInputs(root.FullPath, project);
                string nodeIntermediate = Path.Combine(privateRoot, SafeSegment(node.ProjectId), SafeSegment(node.Profile.Id));
                Directory.CreateDirectory(nodeIntermediate);
                ValidateNoReparsePoints(privateRoot, nodeIntermediate);
                string defines = CanonicalDefineUnion(baseDefines[node.ProjectId], node.Profile.AdditionalSymbols);
                ProcessCapture capture = await RunCheckedAsync(
                    CreateCaptureQuery(
                        root.FullPath,
                        project,
                        paths,
                        captureTarget,
                        nodeIntermediate,
                        defines),
                    cancellationToken).ConfigureAwait(false);
                CapturedCompilerInputs parsed = MsBuildCaptureProtocol.Parse(capture.StandardOutput);
                string capturedTargetFramework = parsed.Properties["TargetFramework"];
                if (!string.Equals(capturedTargetFramework, node.TargetFramework, StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"MSBuild returned target framework '{capturedTargetFramework}' for '{node.Key}', expected '{node.TargetFramework}'.");
                }
                ValidatePrivateGeneratedFile(
                    privateRoot,
                    nodeIntermediate,
                    parsed.Properties["GeneratedAssemblyInfoFile"]);
                ValidatePrivateGeneratedFile(
                    privateRoot,
                    nodeIntermediate,
                    parsed.Properties["TargetFrameworkMonikerAssemblyAttributesPath"]);
                captures.Add(node.Key, parsed);
                string projectDirectory = Path.GetDirectoryName(paths.ProjectFile)
                    ?? throw new InvalidDataException("Project path has no directory.");
                CompilerInputModel normalized = CSharpCommandLineInputParser.Parse(
                    parsed,
                    root.FullPath,
                    projectDirectory,
                    nodeIntermediate,
                    node.Profile.AdditionalSymbols,
                    new CompilerToolchainIdentity(parsed.Properties["RoslynAssembliesPath"]));
                compilerInputs.Add(node.Key, normalized);
            }

            CompilationGraphHashEnvironment environment = new(
                graph,
                captures,
                compilerInputs,
                root.FullPath,
                privateRoot,
                captureTarget);
            CompilationGraphHashInputs hashInputs = _hashInputBuilder is null
                ? BuildHashInputs(environment)
                : _hashInputBuilder(environment) ?? throw new InvalidDataException("The hash-input builder returned null.");
            RepositoryCompilationGraph capturedGraph = graph.WithCaptures(captures).WithCompilerInputs(compilerInputs);
            return capturedGraph.WithHashInputs(hashInputs).WithHashes(CompilationGraphHashing.Compute(hashInputs));
        }
        finally
        {
            DeletePrivateDirectory(privateRoot);
        }
    }

    private async ValueTask<ProcessCapture> RunCheckedAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken)
    {
        ProcessCapture capture = await _runner.RunAsync(
            startInfo,
            QueryTimeout,
            MaximumStandardOutputBytes,
            cancellationToken).ConfigureAwait(false);
        if (capture.ExitCode != 0)
        {
            string output = Encoding.UTF8.GetString(capture.StandardOutput);
            throw new InvalidDataException(
                $"MSBuild exited with code {capture.ExitCode}. stderr: {capture.StandardError.Trim()} stdout: {output.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(capture.StandardError))
            throw new InvalidDataException($"MSBuild wrote to standard error: {capture.StandardError.Trim()}");
        if (capture.StandardOutput.Length > MaximumStandardOutputBytes)
            throw new InvalidDataException("MSBuild standard output exceeded the configured limit.");
        return capture;
    }

    private static ProcessStartInfo CreateBasePropertyQuery(
        string repositoryRoot,
        RepositoryProjectDefinition project,
        ProjectPaths paths)
    {
        ProcessStartInfo start = CreateStartInfo(repositoryRoot, paths.ProjectFile);
        AddCommonEvaluationArguments(start, project, paths);
        start.ArgumentList.Add("-getProperty:DefineConstants,TargetFramework");
        return start;
    }

    private static ProcessStartInfo CreateCaptureQuery(
        string repositoryRoot,
        RepositoryProjectDefinition project,
        ProjectPaths paths,
        string captureTarget,
        string intermediateDirectory,
        string defineConstants)
    {
        ProcessStartInfo start = CreateStartInfo(repositoryRoot, paths.ProjectFile);
        AddCommonEvaluationArguments(start, project, paths);
        start.ArgumentList.Add("/t:_PanGlossCaptureCompilerInputs");
        start.ArgumentList.Add("/p:SkipCompilerExecution=true");
        start.ArgumentList.Add("/p:ProvideCommandLineArgs=true");
        start.ArgumentList.Add($"/p:CustomAfterMicrosoftCommonTargets={captureTarget}");
        start.ArgumentList.Add($"/p:IntermediateOutputPath={Path.TrimEndingDirectorySeparator(intermediateDirectory)}{Path.DirectorySeparatorChar}");
        start.ArgumentList.Add($"/p:DefineConstants={defineConstants.Replace(";", "%3B", StringComparison.Ordinal)}");
        start.ArgumentList.Add($"-getProperty:{CaptureProperties}");
        start.ArgumentList.Add($"-getItem:{CaptureItems}");
        return start;
    }

    private static ProcessStartInfo CreateStartInfo(string repositoryRoot, string projectFile)
    {
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add("msbuild");
        start.ArgumentList.Add(projectFile);
        start.ArgumentList.Add("--noAutoResponse");
        start.ArgumentList.Add("/nologo");
        start.ArgumentList.Add("/nr:false");
        start.ArgumentList.Add("/v:quiet");
        return start;
    }

    private static void AddCommonEvaluationArguments(
        ProcessStartInfo start,
        RepositoryProjectDefinition project,
        ProjectPaths paths)
    {
        start.ArgumentList.Add("/p:Configuration=Release");
        start.ArgumentList.Add($"/p:TargetFramework={project.TargetFramework}");
        start.ArgumentList.Add("/p:BuildProjectReferences=false");
        start.ArgumentList.Add("/p:RestoreIgnoreFailedSources=false");
        start.ArgumentList.Add($"/p:MSBuildProjectExtensionsPath={Path.TrimEndingDirectorySeparator(paths.ObjDirectory)}{Path.DirectorySeparatorChar}");
        start.ArgumentList.Add($"/p:ProjectAssetsFile={paths.AssetsFile}");
    }

    private static string[] ParseBaseDefines(byte[] json, string expectedTargetFramework)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement properties = document.RootElement.GetProperty("Properties");
            string? value = properties.GetProperty("DefineConstants").GetString();
            string? targetFramework = properties.GetProperty("TargetFramework").GetString();
            if (value is null)
                throw new InvalidDataException("MSBuild returned a null DefineConstants property.");
            if (!string.Equals(targetFramework, expectedTargetFramework, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"MSBuild base evaluation returned target framework '{targetFramework}', expected '{expectedTargetFramework}'.");
            }
            return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidDataException("MSBuild base-property output is invalid.", exception);
        }
    }

    private static string CanonicalDefineUnion(
        IReadOnlyCollection<string> baseDefines,
        IReadOnlyCollection<string> additionalDefines) =>
        string.Join(
            ";",
            baseDefines.Concat(additionalDefines)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal));

    private static CompilationGraphHashInputs BuildHashInputs(CompilationGraphHashEnvironment environment)
    {
        RepositoryCompilationGraph graph = environment.Graph;
        IReadOnlyDictionary<RepositoryGraphNodeKey, CapturedCompilerInputs> captures = environment.Captures;
        IReadOnlyDictionary<RepositoryGraphNodeKey, CompilerInputModel> compilerInputs = environment.CompilerInputs;
        string repositoryRoot = environment.RepositoryRoot;
        string privateRoot = environment.PrivateRoot;
        string captureTarget = environment.CaptureTarget;
        ProjectPaths[] projectPaths = graph.Projects
            .Select(project => ValidateProjectInputs(repositoryRoot, project))
            .ToArray();
        LogicalPathRoots roots = InferLogicalRoots(repositoryRoot, privateRoot, captures.Values, projectPaths);
        GraphHashFile targetFile = ReadHashFile(captureTarget, roots, GraphHashFileKind.Text);

        var projects = graph.Projects.Select(project => new ProjectHashInput(
            project.Id,
            LogicalPathTokens.FromAbsolute(Path.Combine(repositoryRoot, project.RelativePath), roots),
            project.TargetFramework)).ToArray();
        var profiles = graph.Profiles.Select(profile => new ProfileHashInput(profile.Id, profile.AdditionalSymbols)).ToArray();
        var nodes = new List<NodeHashInput>(graph.Nodes.Count);
        foreach (RepositoryGraphNode node in graph.Nodes)
        {
            CapturedCompilerInputs capture = captures[node.Key];
            CompilerInputModel input = compilerInputs[node.Key];
            RepositoryProjectDefinition project = graph.Projects.Single(item => item.Id == node.ProjectId);
            ProjectPaths paths = projectPaths.Single(item => string.Equals(
                item.ProjectFile,
                Path.GetFullPath(project.RelativePath, repositoryRoot),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
            nodes.Add(BuildNodeHashInput(graph, node, capture, input, compilerInputs, paths, roots, repositoryRoot, privateRoot));
        }

        OptionalGraphHashFile lockFile = ReadLockFile(projectPaths, roots);
        ToolchainHashInput toolchain = BuildToolchainHashInput(captures.Values, roots);
        return new CompilationGraphHashInputs(
            "hc-compilation-graph/v1",
            projects,
            profiles,
            nodes,
            graph.ProjectEdges,
            toolchain,
            targetFile,
            lockFile);
    }

    private static NodeHashInput BuildNodeHashInput(
        RepositoryCompilationGraph graph,
        RepositoryGraphNode node,
        CapturedCompilerInputs capture,
        CompilerInputModel input,
        IReadOnlyDictionary<RepositoryGraphNodeKey, CompilerInputModel> compilerInputs,
        ProjectPaths paths,
        LogicalPathRoots roots,
        string repositoryRoot,
        string privateRoot)
    {
        var settings = capture.Properties.ToDictionary(
            pair => pair.Key,
            pair => NormalizeStableValue(pair.Key, pair.Value, roots),
            StringComparer.Ordinal);
        settings["ProfileSymbols"] = string.Join(";", node.Profile.AdditionalSymbols.OrderBy(symbol => symbol, StringComparer.Ordinal));

        var arguments = capture.Items["CscCommandLineArgs"]
            .Select((item, ordinal) => new OrderedHashValue(ordinal, NormalizeArgument(item.Identity, roots)))
            .ToArray();
        var sources = input.Sources.Select((source, ordinal) => new OrderedGraphHashFile(
            ordinal,
            new GraphHashFile(
                LogicalPathTokens.FromAbsolute(source.Path, roots),
                source.Content,
                GraphHashFileKind.Text))).ToArray();

        var references = BuildReferenceInputs(graph, node, compilerInputs, capture, roots, repositoryRoot);
        var projectReferences = BuildProjectReferences(graph, node, capture, repositoryRoot);
        var analyzers = BuildAnalyzerInputs(input, roots, repositoryRoot, node.TargetFramework);
        var additionalFiles = BuildAuxiliaryFiles(input.AdditionalFiles, capture.Items["AdditionalFiles"], roots, repositoryRoot, GraphHashFileKind.Text);
        var editorConfigFiles = BuildAuxiliaryFiles(
            input.AnalyzerConfigs,
            capture.Items["EditorConfigFiles"],
            roots,
            repositoryRoot,
            GraphHashFileKind.Text,
            admitAncestorEditorConfig: true);
        if (editorConfigFiles.Count == 0)
        {
            string generatedConfig = Path.Combine(privateRoot, SafeSegment(node.ProjectId), SafeSegment(node.Profile.Id), ".editorconfig");
            string repositoryConfig = Path.Combine(repositoryRoot, ".editorconfig");
            if (!File.Exists(repositoryConfig))
                throw new FileNotFoundException("Fallback analyzer configuration is missing.", repositoryConfig);
            editorConfigFiles.Add(new GraphHashFile(
                LogicalPathTokens.FromAbsolute(generatedConfig, roots),
                ImmutableArray.CreateRange(File.ReadAllBytes(repositoryConfig)),
                GraphHashFileKind.Text));
        }

        ProjectPaths projectPath = paths;
        var assets = new List<GraphHashFile>
        {
            ReadHashFile(projectPath.AssetsFile, roots, GraphHashFileKind.Json),
        };
        string projectName = Path.GetFileNameWithoutExtension(projectPath.ProjectFile);
        assets.Add(ReadHashFile(Path.Combine(projectPath.ObjDirectory, $"{projectName}.csproj.nuget.g.props"), roots, GraphHashFileKind.Text));
        assets.Add(ReadHashFile(Path.Combine(projectPath.ObjDirectory, $"{projectName}.csproj.nuget.g.targets"), roots, GraphHashFileKind.Text));

        var imports = new List<GraphHashFile>();
        foreach (string import in SplitMsBuildPaths(capture.Properties["MSBuildAllProjects"]))
        {
            string path = ResolveCapturePath(import, roots, Path.GetDirectoryName(projectPath.ProjectFile)!);
            imports.Add(ReadHashFile(path, roots, GraphHashFileKind.Text));
        }
        if (imports.Count == 0)
            throw new InvalidDataException($"MSBuildAllProjects for '{node.Key}' did not contain a file import.");

        return new NodeHashInput(
            node.Key,
            settings,
            arguments,
            sources,
            references,
            projectReferences,
            analyzers,
            additionalFiles,
            editorConfigFiles,
            Array.Empty<GraphHashFile>(),
            assets,
            imports);
    }

    private static List<ReferenceHashInput> BuildReferenceInputs(
        RepositoryCompilationGraph graph,
        RepositoryGraphNode node,
        IReadOnlyDictionary<RepositoryGraphNodeKey, CompilerInputModel> compilerInputs,
        CapturedCompilerInputs capture,
        LogicalPathRoots roots,
        string repositoryRoot)
    {
        var result = new List<ReferenceHashInput>();
        IReadOnlyList<CapturedCompilerItem> captured = capture.Items["ReferencePathWithRefAssemblies"];
        var owned = TransitiveOwnedProjects(graph, node.ProjectId).ToHashSet(StringComparer.Ordinal);
        var everyOwnedAssemblyName = graph.Projects.ToDictionary(
            project => project.Id,
            project => ProjectAssemblyName(project.Id, compilerInputs[graph.Nodes.First(item =>
                item.ProjectId == project.Id && item.Profile.Id == node.Profile.Id).Key]),
            StringComparer.Ordinal);
        var admittedOwnedAssemblyNames = everyOwnedAssemblyName
            .Where(pair => owned.Contains(pair.Key))
            .ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);
        foreach (CapturedCompilerItem item in captured)
        {
            string identity = item.Identity;
            IReadOnlyDictionary<string, string> metadata = item.Metadata;
            string capturedPath = metadata.TryGetValue("FullPath", out string? fullPath) ? fullPath : identity;
            string path = ResolveCapturePath(capturedPath, roots, repositoryRoot);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (admittedOwnedAssemblyNames.ContainsKey(fileName))
                continue;
            if (everyOwnedAssemblyName.Values.Any(name => string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException($"Owned binary reference '{capturedPath}' was not matched to an admitted project edge.");
            string assemblyIdentity = metadata.TryGetValue("FusionName", out string? fusionName) && !string.IsNullOrWhiteSpace(fusionName)
                ? fusionName
                : metadata.TryGetValue("Name", out string? name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : identity is not null && !Path.IsPathFullyQualified(identity)
                        ? identity
                        : Path.GetFileNameWithoutExtension(path);
            string logical = LogicalPathTokens.FromAbsolute(path, roots);
            var aliases = metadata.TryGetValue("Aliases", out string? aliasesText)
                ? aliasesText.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : Array.Empty<string>();
            bool embedInterop = metadata.TryGetValue("EmbedInteropTypes", out string? embed)
                && bool.TryParse(embed, out bool parsedEmbed) && parsedEmbed;
            result.Add(new ReferenceHashInput(
                assemblyIdentity,
                new GraphHashFile(logical, ImmutableArray.CreateRange(File.ReadAllBytes(path)), GraphHashFileKind.Binary),
                aliases,
                embedInterop));
        }
        return result;
    }

    private static IEnumerable<string> TransitiveOwnedProjects(RepositoryCompilationGraph graph, string projectId)
    {
        var outgoing = graph.ProjectEdges
            .GroupBy(edge => edge.FromProjectId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.ToProjectId), StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>(outgoing.TryGetValue(projectId, out IEnumerable<string>? direct)
            ? direct
            : Array.Empty<string>());
        while (pending.Count != 0)
        {
            string project = pending.Pop();
            if (!visited.Add(project))
                continue;
            yield return project;
            if (outgoing.TryGetValue(project, out IEnumerable<string>? dependencies))
            {
                foreach (string dependency in dependencies)
                    pending.Push(dependency);
            }
        }
    }

    private static string ProjectAssemblyName(string projectId, CompilerInputModel input)
    {
        if (input.Arguments.CompilationName is { Length: > 0 } commandLineName)
            return commandLineName;
        if (input.Arguments.OutputFileName is { Length: > 0 } outputFile)
            return Path.GetFileNameWithoutExtension(outputFile);
        return projectId switch
        {
            "machine" => "SIL.Machine",
            "hc" => "SIL.Machine.Morphology.HermitCrab",
            "hc-tool" => "SIL.Machine.Morphology.HermitCrab.Tool",
            "hc-conformance" => "hc-conformance",
            _ => projectId,
        };
    }

    private static List<ProjectReferenceHashInput> BuildProjectReferences(
        RepositoryCompilationGraph graph,
        RepositoryGraphNode node,
        CapturedCompilerInputs capture,
        string repositoryRoot)
    {
        var result = new List<ProjectReferenceHashInput>();
        foreach (CapturedCompilerItem item in capture.Items["ProjectReference"])
        {
            string path = item.Metadata.TryGetValue("FullPath", out string? fullPath) ? fullPath : item.Identity;
            string? projectId = graph.Projects
                .Where(project => string.Equals(
                Path.GetFullPath(path, repositoryRoot),
                    Path.GetFullPath(project.RelativePath, repositoryRoot),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .Select(project => project.Id)
                .SingleOrDefault();
            if (projectId is null)
                throw new InvalidDataException($"Project reference '{path}' is outside the fixed project table.");
            result.Add(new ProjectReferenceHashInput(projectId, item.Metadata));
        }
        if (result.Count == 0)
        {
            RepositoryProjectDefinition project = graph.Projects.Single(item => item.Id == node.ProjectId);
            result.AddRange(project.DirectOwnedReferences.Select(reference => new ProjectReferenceHashInput(reference)));
        }
        return result;
    }

    private static List<AnalyzerHashInput> BuildAnalyzerInputs(
        CompilerInputModel input,
        LogicalPathRoots roots,
        string repositoryRoot,
        string targetFramework)
    {
        var inspections = input.Analyzers.ToList();
        if (inspections.Count == 0 && targetFramework == "net10.0")
        {
            string? generatorDirectory = FindSdkGeneratorDirectory(roots.SdkRoot);
            if (generatorDirectory is not null)
            {
                foreach (string name in KnownSdkGeneratorNames)
                {
                    string path = Path.Combine(generatorDirectory, name);
                    if (File.Exists(path))
                        inspections.Add(AnalyzerMetadataInspector.Inspect(path, new[] { generatorDirectory }));
                }
            }
        }
        var result = new List<AnalyzerHashInput>();
        foreach (AnalyzerMetadataInspection inspection in inspections)
        {
            string path = ResolveCapturePath(inspection.Path, roots, repositoryRoot);
            result.Add(new AnalyzerHashInput(
                inspection.AssemblyIdentity,
                new GraphHashFile(
                    LogicalPathTokens.FromAbsolute(path, roots),
                    ImmutableArray.CreateRange(File.ReadAllBytes(path)),
                    GraphHashFileKind.Binary)));
        }
        return result;
    }

    private static List<GraphHashFile> BuildAuxiliaryFiles(
        IReadOnlyList<CompilerAuxiliaryInput> inputs,
        IReadOnlyList<CapturedCompilerItem> captured,
        LogicalPathRoots roots,
        string repositoryRoot,
        GraphHashFileKind kind,
        bool admitAncestorEditorConfig = false)
    {
        var result = new List<GraphHashFile>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string path, ImmutableArray<byte> content) in inputs.Select(item => (item.Path, item.Content)).Concat(
            captured.Select(item =>
            {
                string path = item.Metadata.TryGetValue("FullPath", out string? fullPath) ? fullPath : item.Identity;
                path = ResolveCapturePath(path, roots, repositoryRoot);
                return (path, ImmutableArray.CreateRange(File.ReadAllBytes(path)));
            })))
        {
            // Only EditorConfigFiles admits an ancestor .editorconfig; every other auxiliary kind still fails closed.
            string logical = admitAncestorEditorConfig
                ? LogicalPathTokens.FromAbsoluteAdmittingAncestorEditorConfig(path, roots)
                : LogicalPathTokens.FromAbsolute(path, roots);
            if (seen.Add(logical))
                result.Add(new GraphHashFile(logical, content, kind));
        }
        return result;
    }

    private static readonly string[] KnownSdkGeneratorNames =
    {
        "Microsoft.Interop.ComInterfaceGenerator.dll",
        "Microsoft.Interop.JavaScript.JSImportGenerator.dll",
        "Microsoft.Interop.LibraryImportGenerator.dll",
        "System.Text.Json.SourceGeneration.dll",
        "System.Text.RegularExpressions.Generator.dll",
    };

    private static ToolchainHashInput BuildToolchainHashInput(
        IEnumerable<CapturedCompilerInputs> captures,
        LogicalPathRoots roots)
    {
        CapturedCompilerInputs first = captures.First();
        string roslynPath = first.Properties["RoslynAssembliesPath"];
        string actualRoslynPath = Path.IsPathFullyQualified(roslynPath)
            ? Path.GetFullPath(roslynPath)
            : Path.GetDirectoryName(typeof(CSharpCommandLineInputParser).Assembly.Location)!
                ?? throw new InvalidDataException("The loaded Roslyn assembly has no directory.");
        string roslynLogical = LogicalPathTokens.IsLogicalPath(roslynPath)
            ? NormalizeLogicalPath(roslynPath)
            : LogicalPathTokens.FromAbsolute(actualRoslynPath, roots);
        var files = new List<GraphHashFile>();
        foreach (string name in new[] { "Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.CSharp.dll" })
        {
            string path = Path.Combine(actualRoslynPath, name);
            if (!File.Exists(path))
                continue;
            files.Add(ReadHashFile(path, roots, GraphHashFileKind.Binary));
        }
        if (files.Count == 0)
            throw new FileNotFoundException("The captured Roslyn toolchain contains no compiler assemblies.", actualRoslynPath);

        string compilerPath = first.Properties["CscToolPath"];
        string compilerIdentity = string.IsNullOrWhiteSpace(compilerPath)
            ? "csc:dotnet"
            : "csc:" + NormalizeStableValue("CscToolPath", compilerPath, roots);
        string loaderAssembly = typeof(RepositoryCompilationGraphLoader).Assembly.Location;
        string loaderIdentity = typeof(RepositoryCompilationGraphLoader).Assembly.GetName().FullName ??
            typeof(RepositoryCompilationGraphLoader).FullName!;
        if (File.Exists(loaderAssembly))
        {
            loaderIdentity += "|mvid:" + typeof(RepositoryCompilationGraphLoader).Module.ModuleVersionId.ToString("D");
            files.Add(ReadHashFile(loaderAssembly, roots, GraphHashFileKind.Binary));
        }
        return new ToolchainHashInput(
            first.Properties["NETCoreSdkVersion"],
            first.Properties["MSBuildVersion"],
            "roslyn:" + roslynLogical,
            compilerIdentity,
            loaderIdentity,
            files);
    }

    private static OptionalGraphHashFile ReadLockFile(
        IReadOnlyList<ProjectPaths> paths,
        LogicalPathRoots roots)
    {
        foreach (ProjectPaths project in paths)
        {
            string lockPath = Path.Combine(project.ObjDirectory, "packages.lock.json");
            if (File.Exists(lockPath))
                return new OptionalGraphHashFile(true, ReadHashFile(lockPath, roots, GraphHashFileKind.Json));
        }
        return new OptionalGraphHashFile(false, null);
    }

    private static LogicalPathRoots InferLogicalRoots(
        string repositoryRoot,
        string privateRoot,
        IEnumerable<CapturedCompilerInputs> captures,
        IReadOnlyList<ProjectPaths> projectPaths)
    {
        var sdkCandidates = new List<string>();
        foreach (CapturedCompilerInputs capture in captures)
        {
            string roslyn = capture.Properties["RoslynAssembliesPath"];
            if (Path.IsPathFullyQualified(roslyn))
                sdkCandidates.Add(roslyn);
            foreach (CapturedCompilerItem reference in capture.Items["ReferencePathWithRefAssemblies"])
            {
                string path = reference.Metadata.TryGetValue("FullPath", out string? fullPath) ? fullPath : reference.Identity;
                if (Path.IsPathFullyQualified(path))
                    sdkCandidates.Add(path);
            }
        }
        string parserPath = typeof(CSharpCommandLineInputParser).Assembly.Location;
        if (Path.IsPathFullyQualified(parserPath))
            sdkCandidates.Add(parserPath);
        string? sdkRoot = sdkCandidates
            .Select(FindDotnetRoot)
            .FirstOrDefault(path => path is not null);
        if (sdkRoot is null)
            throw new InvalidDataException("Unable to infer a unique SDK root from captured compiler inputs.");

        var nugetRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ProjectPaths project in projectPaths)
        {
            using JsonDocument assets = JsonDocument.Parse(File.ReadAllBytes(project.AssetsFile));
            if (assets.RootElement.TryGetProperty("packageFolders", out JsonElement folders) && folders.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty folder in folders.EnumerateObject())
                {
                    if (Path.IsPathFullyQualified(folder.Name))
                    {
                        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder.Name));
                        ValidateNoReparsePoints(root, root);
                        nugetRoots.Add(root);
                    }
                }
            }
        }
        foreach (CapturedCompilerInputs capture in captures)
        {
            foreach (CapturedCompilerItem reference in capture.Items["ReferencePathWithRefAssemblies"])
            {
                string path = reference.Metadata.TryGetValue("FullPath", out string? fullPath) ? fullPath : reference.Identity;
                if (!Path.IsPathFullyQualified(path))
                    continue;
                string? root = FindPackageRoot(path);
                if (root is null)
                    continue;
                ValidateNoReparsePoints(root, Path.GetFullPath(path));
                nugetRoots.Add(root);
            }
        }
        if (nugetRoots.Count == 0)
            throw new InvalidDataException("Unable to infer a NuGet package root from restored assets or references.");
        string[] distinctNuGetRoots = nugetRoots
            .Select(root => LogicalPathTokens.NormalizeAbsolute(Path.GetFullPath(root)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(root => root, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new LogicalPathRoots(repositoryRoot, sdkRoot, distinctNuGetRoots, privateRoot);
    }

    private static string? FindPackageRoot(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string[] segments = LogicalPathTokens.NormalizeAbsolute(fullPath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            if (!string.Equals(segments[i], "packages", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(segments[i], "NuGetFallbackFolder", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            string root = fullPath.StartsWith("/", StringComparison.Ordinal)
                ? "/" + string.Join('/', segments.Take(i + 1))
                : string.Join('/', segments.Take(i + 1));
            if (root.Length == 2 && root[1] == ':')
                root += "/";
            root = LogicalPathTokens.NormalizeAbsolute(root);
            string relative = fullPath.Length == root.Length ? string.Empty : fullPath[(root.Length + 1)..];
            string[] package = relative.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (package.Length >= 2 && IsPackageSegment(package[0]) && IsPackageSegment(package[1]) && Directory.Exists(root))
                return root;
        }
        return null;
    }

    private static bool IsPackageSegment(string value) =>
        !string.IsNullOrWhiteSpace(value) && value is not "." and not ".." &&
        value.IndexOfAny(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) < 0;

    private static string? FindDotnetRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !LogicalPathTokens.IsAbsolute(path))
            return null;
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            return null;
        }
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            return null;
        string normalized = LogicalPathTokens.NormalizeAbsolute(fullPath);
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int sdkIndex = Array.FindIndex(segments, segment => string.Equals(segment, "sdk", StringComparison.OrdinalIgnoreCase));
        if (sdkIndex < 0)
            return null;
        string root = normalized.StartsWith("/", StringComparison.Ordinal)
            ? "/" + string.Join('/', segments.Take(sdkIndex))
            : string.Join('/', segments.Take(sdkIndex));
        if (root.Length == 2 && root[1] == ':')
            root += "/";
        string normalizedRoot = LogicalPathTokens.NormalizeAbsolute(root);
        try
        {
            ValidateNoReparsePoints(normalizedRoot, fullPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return null;
        }
        return normalizedRoot;
    }

    private static string? FindSdkGeneratorDirectory(string sdkRoot)
    {
        string packs = Path.Combine(sdkRoot, "packs", "Microsoft.NETCore.App.Ref");
        if (!Directory.Exists(packs))
            return null;
        return Directory.EnumerateDirectories(packs)
            .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
            .Select(path => Path.Combine(path, "analyzers", "dotnet", "cs"))
            .FirstOrDefault(Directory.Exists);
    }

    private static GraphHashFile ReadHashFile(
        string path,
        LogicalPathRoots roots,
        GraphHashFileKind kind)
    {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Required hash input file is missing.", fullPath);
        string admittedRoot = FindAdmittedRoot(fullPath, roots);
        ValidateNoReparsePoints(admittedRoot, fullPath);
        return new GraphHashFile(
            LogicalPathTokens.FromAbsolute(fullPath, roots),
            ImmutableArray.CreateRange(File.ReadAllBytes(fullPath)),
            kind);
    }

    private static string FindAdmittedRoot(string fullPath, LogicalPathRoots roots)
    {
        string normalizedPath = LogicalPathTokens.NormalizeAbsolute(fullPath);
        var candidates = new List<(string Root, bool IsNuGet, string? PackageIdentity)>();
        AddCandidate(roots.RepositoryRoot, false);
        AddCandidate(roots.SdkRoot, false);
        AddCandidate(roots.GeneratedRoot, false);
        foreach (string root in roots.NuGetRoots)
            AddCandidate(root, true);
        if (candidates.Count == 0)
            throw new InvalidDataException($"Hash input '{fullPath}' is outside all admitted roots.");
        if (candidates.Count == 1)
            return candidates[0].Root;
        if (!candidates.All(candidate => candidate.IsNuGet) ||
            candidates.Select(candidate => candidate.PackageIdentity).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
        {
            throw new InvalidDataException($"Hash input '{fullPath}' has ambiguous admitted roots.");
        }
        return candidates.OrderBy(candidate => candidate.Root, StringComparer.OrdinalIgnoreCase).First().Root;

        void AddCandidate(string root, bool isNuGet)
        {
            if (!LogicalPathTokens.IsUnder(normalizedPath, root))
                return;
            string? identity = null;
            if (isNuGet)
            {
                string relative = normalizedPath.Length == root.Length ? string.Empty : normalizedPath[(root.Length + 1)..];
                string[] segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 2)
                    throw new InvalidDataException($"NuGet hash input '{fullPath}' is not decomposable into package ID and version.");
                identity = segments[0] + "/" + segments[1];
            }
            candidates.Add((root, isNuGet, identity));
        }
    }

    private static string ResolveCapturePath(string path, LogicalPathRoots roots, string baseDirectory)
    {
        if (path.StartsWith("tmp:/", StringComparison.Ordinal))
            return Path.Combine(roots.GeneratedRoot, path[5..].Replace('/', Path.DirectorySeparatorChar));
        if (LogicalPathTokens.IsLogicalPath(path))
        {
            string[] prefix = { "repo:/", "sdk:/", "nuget:/", "generated:/" };
            string token = prefix.Single(path.StartsWith);
            string root = token switch
            {
                "repo:/" => roots.RepositoryRoot,
                "sdk:/" => roots.SdkRoot,
                "nuget:/" => ResolveNuGetLogicalRoot(path[token.Length..], roots),
                _ => roots.GeneratedRoot,
            };
            return Path.Combine(root, path[token.Length..].Replace('/', Path.DirectorySeparatorChar));
        }
        if (Path.IsPathFullyQualified(path))
            return Path.GetFullPath(path);
        return Path.GetFullPath(path, baseDirectory);
    }

    private static string ResolveNuGetLogicalRoot(string relative, LogicalPathRoots roots)
    {
        string[] segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            throw new InvalidDataException($"NuGet logical path '{relative}' is not decomposable into package ID and version.");
        foreach (string root in roots.NuGetRoots)
        {
            string candidate = Path.Combine(root, segments[0], segments[1]);
            if (Directory.Exists(candidate))
                return root;
        }
        return roots.NuGetRoot;
    }

    private static string NormalizeStableValue(string name, string value, LogicalPathRoots roots)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        if (name == "MSBuildAllProjects")
            return string.Join(";", SplitMsBuildPaths(value).Select(path => NormalizeStablePath(path, roots)));
        return NormalizeStablePath(value, roots);
    }

    private static string NormalizeArgument(string value, LogicalPathRoots roots)
    {
        if (Path.IsPathFullyQualified(value))
            return LogicalPathTokens.FromAbsoluteAdmittingAncestorEditorConfig(Path.GetFullPath(value), roots);
        int separator = value.IndexOf(':');
        if (separator > 0)
        {
            string prefix = value[..(separator + 1)];
            string tail = value[(separator + 1)..];
            string[] parts = tail.Split('=', 2);
            if (Path.IsPathFullyQualified(parts[^1]))
            {
                parts[^1] = LogicalPathTokens.FromAbsoluteAdmittingAncestorEditorConfig(Path.GetFullPath(parts[^1]), roots);
                return prefix + string.Join('=', parts);
            }
        }
        return value;
    }

    private static string NormalizeStablePath(string value, LogicalPathRoots roots)
    {
        if (value.StartsWith("tmp:/", StringComparison.Ordinal))
            return "generated:/" + value[5..];
        if (LogicalPathTokens.IsLogicalPath(value))
            return NormalizeLogicalPath(value);
        if (Path.IsPathFullyQualified(value))
            return LogicalPathTokens.FromAbsolute(Path.GetFullPath(value), roots);
        return value;
    }

    private static string NormalizeLogicalPath(string value) =>
        value.Replace('\\', '/');

    private static IEnumerable<string> SplitMsBuildPaths(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static ProjectPaths ValidateProjectInputs(
        string repositoryRoot,
        RepositoryProjectDefinition project)
    {
        string projectFile = ResolveRepositoryFile(repositoryRoot, project.RelativePath);
        string projectDirectory = Path.GetDirectoryName(projectFile)
            ?? throw new InvalidDataException("Project path has no directory.");
        string objDirectory = ResolveRepositoryDirectory(repositoryRoot, Path.Combine(
            Path.GetRelativePath(repositoryRoot, projectDirectory),
            "obj"));
        string assetsFile = ResolveRepositoryFile(repositoryRoot, Path.Combine(
            Path.GetRelativePath(repositoryRoot, objDirectory),
            "project.assets.json"));
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        ResolveRepositoryFile(repositoryRoot, Path.Combine(
            Path.GetRelativePath(repositoryRoot, objDirectory),
            $"{projectName}.csproj.nuget.g.props"));
        ResolveRepositoryFile(repositoryRoot, Path.Combine(
            Path.GetRelativePath(repositoryRoot, objDirectory),
            $"{projectName}.csproj.nuget.g.targets"));
        ValidateAssetsTarget(assetsFile, project.TargetFramework);
        return new ProjectPaths(projectFile, objDirectory, assetsFile);
    }

    private static void ValidateAssetsTarget(string assetsFile, string targetFramework)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(assetsFile));
            JsonElement frameworks = document.RootElement.GetProperty("project").GetProperty("frameworks");
            if (!frameworks.TryGetProperty(targetFramework, out _))
                throw new InvalidDataException($"Assets file does not contain target framework '{targetFramework}'.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Assets file '{assetsFile}' is invalid JSON.", exception);
        }
    }

    private static string ResolveRepositoryFile(string repositoryRoot, string relativePath)
    {
        string path = ResolveContainedPath(repositoryRoot, relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException("Required repository file is missing.", path);
        ValidateNoReparsePoints(repositoryRoot, path);
        return path;
    }

    private static string ResolveRepositoryDirectory(string repositoryRoot, string relativePath)
    {
        string path = ResolveContainedPath(repositoryRoot, relativePath);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Required repository directory '{path}' is missing.");
        ValidateNoReparsePoints(repositoryRoot, path);
        return path;
    }

    private static string ResolveContainedPath(string repositoryRoot, string relativePath)
    {
        if (Path.IsPathFullyQualified(relativePath))
            throw new InvalidDataException("Repository paths must be relative.");
        string fullPath = Path.GetFullPath(relativePath, repositoryRoot);
        string prefix = Path.TrimEndingDirectorySeparator(repositoryRoot) + Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!fullPath.StartsWith(prefix, comparison))
            throw new InvalidDataException($"Repository path '{relativePath}' escapes the root.");
        return fullPath;
    }

    private static void ValidateRepositoryRoot(string repositoryRoot)
    {
        if (!Directory.Exists(repositoryRoot))
            throw new DirectoryNotFoundException($"Repository root '{repositoryRoot}' is missing.");
        FileAttributes attributes = File.GetAttributes(repositoryRoot);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Repository root cannot be a reparse point.");
    }

    private static void ValidateNoReparsePoints(string root, string path)
    {
        string relative = Path.GetRelativePath(root, path);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathFullyQualified(relative))
            throw new InvalidDataException("Path escapes its validated root.");
        string current = root;
        foreach (string segment in relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Reparse point '{current}' is not admitted.");
        }
    }

    internal static void ValidatePrivateGeneratedFile(string privateRoot, string intermediateDirectory, string path)
    {
        string fullPath = Path.GetFullPath(path);
        string relative = Path.GetRelativePath(intermediateDirectory, fullPath);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathFullyQualified(relative))
            throw new InvalidDataException("Generated compiler source escapes its private intermediate directory.");
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Generated compiler source is missing.", fullPath);
        ValidateNoReparsePoints(privateRoot, fullPath);
    }

    private static string SafeSegment(string value)
    {
        if (value.Length == 0 || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidDataException($"Invalid graph path segment '{value}'.");
        return value;
    }

    private static void DeletePrivateDirectory(string privateRoot)
    {
        if (!Directory.Exists(privateRoot))
            return;
        string leaf = Path.GetFileName(Path.TrimEndingDirectorySeparator(privateRoot));
        if (leaf.Length != 32 || !Guid.TryParseExact(leaf, "N", out _))
            throw new InvalidDataException("Refusing to delete an unverified MSBuild query directory.");
        ValidateNoReparsePoints(Path.GetDirectoryName(privateRoot)!, privateRoot);
        Directory.Delete(privateRoot, recursive: true);
    }

    private sealed record ProjectPaths(string ProjectFile, string ObjDirectory, string AssetsFile);
}
