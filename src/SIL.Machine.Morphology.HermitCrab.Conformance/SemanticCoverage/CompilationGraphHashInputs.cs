#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal enum GraphHashFileKind
{
    Text,
    Json,
    Binary,
}

internal sealed record GraphHashFile
{
    internal GraphHashFile(string logicalPath, ImmutableArray<byte> content, GraphHashFileKind kind)
    {
        if (!LogicalPathTokens.IsLogicalPath(logicalPath))
            throw new ArgumentException("A logical path token is required.", nameof(logicalPath));
        if (content.IsDefault)
            throw new ArgumentException("File content must be initialized.", nameof(content));
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        LogicalPath = logicalPath;
        Content = ImmutableArray.CreateRange(content);
        Kind = kind;
    }

    internal string LogicalPath { get; }
    internal ImmutableArray<byte> Content { get; }
    internal GraphHashFileKind Kind { get; }

}

internal sealed record OrderedHashValue
{
    internal OrderedHashValue(int ordinal, string value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Ordinal = ordinal;
        Value = value;
    }

    internal int Ordinal { get; }
    internal string Value { get; }
}

internal sealed record OrderedGraphHashFile
{
    internal OrderedGraphHashFile(int ordinal, GraphHashFile file)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        ArgumentNullException.ThrowIfNull(file);
        Ordinal = ordinal;
        File = file;
    }

    internal int Ordinal { get; }
    internal GraphHashFile File { get; }
}

internal sealed record ProjectHashInput
{
    internal ProjectHashInput(string id, string logicalPath, string targetFramework)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!LogicalPathTokens.IsLogicalPath(logicalPath))
            throw new ArgumentException("A logical path token is required.", nameof(logicalPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFramework);
        Id = id;
        LogicalPath = logicalPath;
        TargetFramework = targetFramework;
    }

    internal string Id { get; }
    internal string LogicalPath { get; }
    internal string TargetFramework { get; }
}

internal sealed record ProfileHashInput
{
    internal ProfileHashInput(string id, IReadOnlyList<string> symbols)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(symbols);
        if (symbols.Any(symbol => string.IsNullOrWhiteSpace(symbol)))
            throw new ArgumentException("Profile symbols must be nonempty.", nameof(symbols));
        Id = id;
        Symbols = ImmutableArray.CreateRange(symbols);
    }

    internal string Id { get; }
    internal ImmutableArray<string> Symbols { get; }
}

internal sealed record ReferenceHashInput
{
    internal ReferenceHashInput(
        string identity,
        GraphHashFile file,
        IReadOnlyList<string> aliases,
        bool embedInteropTypes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(aliases);
        if (aliases.Any(alias => alias is null))
            throw new ArgumentException("Reference aliases cannot contain null elements.", nameof(aliases));
        Identity = identity;
        File = file;
        Aliases = ImmutableArray.CreateRange(aliases);
        EmbedInteropTypes = embedInteropTypes;
    }

    internal string Identity { get; }
    internal GraphHashFile File { get; }
    internal ImmutableArray<string> Aliases { get; }
    internal bool EmbedInteropTypes { get; }
}

internal sealed record ProjectReferenceHashInput
{
    internal ProjectReferenceHashInput(string projectId, IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ProjectId = projectId;
        if (metadata is not null && metadata.Any(pair => pair.Key is null || pair.Value is null))
            throw new ArgumentException("Project reference metadata cannot contain null elements.", nameof(metadata));
        Metadata = (metadata ?? new Dictionary<string, string>())
            .ToImmutableSortedDictionary(StringComparer.Ordinal);
    }

    internal string ProjectId { get; }
    internal ImmutableSortedDictionary<string, string> Metadata { get; }
}

internal sealed record AnalyzerHashInput
{
    internal AnalyzerHashInput(string identity, GraphHashFile file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentNullException.ThrowIfNull(file);
        Identity = identity;
        File = file;
    }

    internal string Identity { get; }
    internal GraphHashFile File { get; }
}

internal sealed record OptionalGraphHashFile
{
    internal OptionalGraphHashFile(bool isPresent, GraphHashFile? file)
    {
        if (isPresent != (file is not null))
            throw new ArgumentException("Optional file presence must agree with its value.");
        IsPresent = isPresent;
        File = file;
    }

    internal bool IsPresent { get; }
    internal GraphHashFile? File { get; }
}

internal sealed record NodeHashInput
{
    internal NodeHashInput(
        RepositoryGraphNodeKey key,
        IReadOnlyDictionary<string, string> settings,
        IReadOnlyList<OrderedHashValue> arguments,
        IReadOnlyList<OrderedGraphHashFile> sources,
        IReadOnlyList<ReferenceHashInput> references,
        IReadOnlyList<ProjectReferenceHashInput> projectReferences,
        IReadOnlyList<AnalyzerHashInput> analyzers,
        IReadOnlyList<GraphHashFile> additionalFiles,
        IReadOnlyList<GraphHashFile> editorConfigFiles,
        IReadOnlyList<GraphHashFile> usings,
        IReadOnlyList<GraphHashFile> assets,
        IReadOnlyList<GraphHashFile> imports)
    {
        ArgumentNullException.ThrowIfNull(key);
        ValidateKeySegment(key.ProjectId, nameof(key.ProjectId));
        ValidateKeySegment(key.TargetFramework, nameof(key.TargetFramework));
        ValidateKeySegment(key.ProfileId, nameof(key.ProfileId));
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(references);
        ArgumentNullException.ThrowIfNull(projectReferences);
        ArgumentNullException.ThrowIfNull(analyzers);
        ArgumentNullException.ThrowIfNull(additionalFiles);
        ArgumentNullException.ThrowIfNull(editorConfigFiles);
        ArgumentNullException.ThrowIfNull(usings);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(imports);
        RejectNull(arguments, nameof(arguments)); RejectNull(sources, nameof(sources));
        RejectNull(references, nameof(references)); RejectNull(projectReferences, nameof(projectReferences));
        RejectNull(analyzers, nameof(analyzers)); RejectNull(additionalFiles, nameof(additionalFiles));
        RejectNull(editorConfigFiles, nameof(editorConfigFiles)); RejectNull(usings, nameof(usings));
        RejectNull(assets, nameof(assets)); RejectNull(imports, nameof(imports));
        Key = key;
        Settings = settings.ToImmutableSortedDictionary(StringComparer.Ordinal);
        Arguments = ImmutableArray.CreateRange(arguments);
        Sources = ImmutableArray.CreateRange(sources);
        References = ImmutableArray.CreateRange(references);
        ProjectReferences = ImmutableArray.CreateRange(projectReferences);
        Analyzers = ImmutableArray.CreateRange(analyzers);
        AdditionalFiles = ImmutableArray.CreateRange(additionalFiles);
        EditorConfigFiles = ImmutableArray.CreateRange(editorConfigFiles);
        Usings = ImmutableArray.CreateRange(usings);
        Assets = ImmutableArray.CreateRange(assets);
        Imports = ImmutableArray.CreateRange(imports);
        ValidateOrdinals(Arguments.Select(argument => argument.Ordinal), nameof(arguments));
        ValidateOrdinals(Sources.Select(source => source.Ordinal), nameof(sources));
        RejectDuplicate(references.Select(reference => reference.Identity), nameof(references));
        RejectDuplicate(projectReferences.Select(reference => reference.ProjectId), nameof(projectReferences));
        RejectDuplicate(analyzers.Select(analyzer => analyzer.Identity), nameof(analyzers));
        ValidateFiles(additionalFiles, nameof(additionalFiles)); ValidateFiles(editorConfigFiles, nameof(editorConfigFiles));
        ValidateFiles(usings, nameof(usings)); ValidateFiles(assets, nameof(assets)); ValidateFiles(imports, nameof(imports));
    }

    internal RepositoryGraphNodeKey Key { get; }
    internal ImmutableSortedDictionary<string, string> Settings { get; }
    internal ImmutableArray<OrderedHashValue> Arguments { get; }
    internal ImmutableArray<OrderedGraphHashFile> Sources { get; }
    internal ImmutableArray<ReferenceHashInput> References { get; }
    internal ImmutableArray<ProjectReferenceHashInput> ProjectReferences { get; }
    internal ImmutableArray<AnalyzerHashInput> Analyzers { get; }
    internal ImmutableArray<GraphHashFile> AdditionalFiles { get; }
    internal ImmutableArray<GraphHashFile> EditorConfigFiles { get; }
    internal ImmutableArray<GraphHashFile> Usings { get; }
    internal ImmutableArray<GraphHashFile> Assets { get; }
    internal ImmutableArray<GraphHashFile> Imports { get; }

    private static void ValidateOrdinals(IEnumerable<int> ordinals, string parameterName)
    {
        int[] actual = ordinals.OrderBy(ordinal => ordinal).ToArray();
        int[] expected = Enumerable.Range(0, actual.Length).ToArray();
        if (!actual.SequenceEqual(expected))
            throw new InvalidDataException($"{parameterName} ordinals must be unique, contiguous, and start at zero.");
    }

    private static void ValidateKeySegment(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("Node identity segments must be nonempty and contain no path separators.", parameterName);
    }

    private static void RejectNull<T>(IEnumerable<T> values, string parameterName) where T : class
    {
        if (values.Any(value => value is null))
            throw new ArgumentException("Collections cannot contain null elements.", parameterName);
    }

    private static void RejectDuplicate(IEnumerable<string> values, string parameterName)
    {
        if (values.GroupBy(value => value, StringComparer.Ordinal).Any(group => group.Count() > 1))
            throw new InvalidDataException($"Collection '{parameterName}' contains duplicate identities.");
    }

    private static void ValidateFiles(IEnumerable<GraphHashFile> values, string parameterName)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (GraphHashFile file in values)
        {
            if (!paths.Add(file.LogicalPath))
                throw new InvalidDataException($"Collection '{parameterName}' contains duplicate logical files.");
        }
    }
}

internal sealed record ToolchainHashInput
{
    internal ToolchainHashInput(
        string sdkVersion,
        string msBuildVersion,
        string roslynIdentity,
        string compilerIdentity,
        string loaderIdentity,
        IReadOnlyList<GraphHashFile> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sdkVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(msBuildVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(roslynIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(compilerIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(loaderIdentity);
        ArgumentNullException.ThrowIfNull(files);
        if (files.Any(file => file is null))
            throw new ArgumentException("Toolchain files cannot contain null elements.", nameof(files));
        if (files.Select(file => file.LogicalPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count)
            throw new InvalidDataException("Toolchain files contain duplicate logical paths.");
        SdkVersion = sdkVersion;
        MSBuildVersion = msBuildVersion;
        RoslynIdentity = roslynIdentity;
        CompilerIdentity = compilerIdentity;
        LoaderIdentity = loaderIdentity;
        Files = ImmutableArray.CreateRange(files);
    }

    internal string SdkVersion { get; }
    internal string MSBuildVersion { get; }
    internal string RoslynIdentity { get; }
    internal string CompilerIdentity { get; }
    internal string LoaderIdentity { get; }
    internal ImmutableArray<GraphHashFile> Files { get; }
}

internal sealed record CompilationGraphHashInputs
{
    internal CompilationGraphHashInputs(
        string schemaVersion,
        IReadOnlyList<ProjectHashInput> projects,
        IReadOnlyList<ProfileHashInput> profiles,
        IReadOnlyList<NodeHashInput> nodes,
        IReadOnlyList<RepositoryProjectEdge> edges,
        ToolchainHashInput toolchain,
        GraphHashFile captureTarget,
        OptionalGraphHashFile lockFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(toolchain);
        ArgumentNullException.ThrowIfNull(captureTarget);
        ArgumentNullException.ThrowIfNull(lockFile);
        SchemaVersion = schemaVersion;
        Projects = ImmutableArray.CreateRange(projects);
        Profiles = ImmutableArray.CreateRange(profiles);
        Nodes = ImmutableArray.CreateRange(nodes);
        Edges = ImmutableArray.CreateRange(edges);
        Toolchain = toolchain;
        CaptureTarget = captureTarget;
        LockFile = lockFile;
        RejectNull(Projects, nameof(projects)); RejectNull(Profiles, nameof(profiles)); RejectNull(Nodes, nameof(nodes));
        RejectNull(Edges, nameof(edges));
        RejectDuplicate(Projects.Select(project => project.Id), "project IDs");
        RejectDuplicate(Projects.Select(project => project.LogicalPath), "project paths");
        RejectDuplicate(Profiles.Select(profile => profile.Id), "profile IDs");
        RejectDuplicate(Nodes.Select(node => node.Key), "node keys");
        if (Edges.Distinct().Count() != Edges.Length)
            throw new InvalidDataException("Edges contain duplicates.");
        ValidateLogicalPaths();
    }

    internal string SchemaVersion { get; }
    internal ImmutableArray<ProjectHashInput> Projects { get; }
    internal ImmutableArray<ProfileHashInput> Profiles { get; }
    internal ImmutableArray<NodeHashInput> Nodes { get; }
    internal ImmutableArray<RepositoryProjectEdge> Edges { get; }
    internal ToolchainHashInput Toolchain { get; }
    internal GraphHashFile CaptureTarget { get; }
    internal OptionalGraphHashFile LockFile { get; }

    private void ValidateLogicalPaths()
    {
        var entries = new Dictionary<string, LogicalPathEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (ProjectHashInput project in Projects)
        {
            if (!entries.TryAdd(project.LogicalPath, new LogicalPathEntry(project.LogicalPath)))
                throw new InvalidDataException($"Logical project path '{project.LogicalPath}' collides with another logical path.");
        }

        foreach (GraphHashFile file in EnumerateAllFiles())
        {
            if (entries.TryGetValue(file.LogicalPath, out LogicalPathEntry? existing))
            {
                if (!string.Equals(existing.LogicalPath, file.LogicalPath, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Logical path '{file.LogicalPath}' has a case-variant collision.");
                }

                if (existing.File is null)
                {
                    existing.File = file;
                }
                else if (existing.File.Kind != file.Kind
                    || !existing.File.Content.AsSpan().SequenceEqual(file.Content.AsSpan()))
                {
                    throw new InvalidDataException($"Logical path '{file.LogicalPath}' has conflicting content or kind.");
                }
            }
            else
            {
                entries.Add(file.LogicalPath, new LogicalPathEntry(file));
            }
        }
    }

    private static void RejectNull<T>(IEnumerable<T> values, string parameterName) where T : class
    {
        if (values.Any(value => value is null))
            throw new ArgumentException("Collections cannot contain null elements.", parameterName);
    }

    private static void RejectDuplicate<T>(IEnumerable<T> values, string description)
    {
        if (values.GroupBy(value => value).Any(group => group.Count() > 1))
            throw new InvalidDataException($"Collection contains duplicate {description}.");
    }

    private static IEnumerable<GraphHashFile> EnumerateFiles(NodeHashInput node)
    {
        foreach (OrderedGraphHashFile file in node.Sources) yield return file.File;
        foreach (ReferenceHashInput reference in node.References) yield return reference.File;
        foreach (AnalyzerHashInput analyzer in node.Analyzers) yield return analyzer.File;
        foreach (GraphHashFile file in node.AdditionalFiles) yield return file;
        foreach (GraphHashFile file in node.EditorConfigFiles) yield return file;
        foreach (GraphHashFile file in node.Usings) yield return file;
        foreach (GraphHashFile file in node.Assets) yield return file;
        foreach (GraphHashFile file in node.Imports) yield return file;
    }

    private IEnumerable<GraphHashFile> EnumerateAllFiles()
    {
        foreach (GraphHashFile file in Nodes.SelectMany(EnumerateFiles)) yield return file;
        foreach (GraphHashFile file in Toolchain.Files) yield return file;
        yield return CaptureTarget;
        if (LockFile.IsPresent)
            yield return LockFile.File!;
    }

    private sealed class LogicalPathEntry
    {
        internal LogicalPathEntry(string projectPath)
        {
            LogicalPath = projectPath;
        }

        internal LogicalPathEntry(GraphHashFile file)
        {
            LogicalPath = file.LogicalPath;
            File = file;
        }

        internal string LogicalPath { get; }
        internal GraphHashFile? File { get; set; }
    }
}
