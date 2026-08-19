#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record RepositoryRoot
{
    internal RepositoryRoot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (!Path.IsPathFullyQualified(fullPath) || !Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Repository root '{fullPath}' does not exist.");
        FullPath = Path.TrimEndingDirectorySeparator(fullPath);
    }

    internal string FullPath { get; }
}

/// <summary>The closed set of build symbols deliberately exercised by conformance.</summary>
internal sealed record BuildProfile
{
    public BuildProfile(string id, IReadOnlyList<string> additionalSymbols)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A build profile ID is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(additionalSymbols);
        if (additionalSymbols.Any(symbol => string.IsNullOrWhiteSpace(symbol)))
            throw new ArgumentException("Profile symbols must be nonempty.", nameof(additionalSymbols));

        Id = id;
        AdditionalSymbols = new ReadOnlyCollection<string>(additionalSymbols.ToArray());
    }

    public string Id { get; }

    public IReadOnlyList<string> AdditionalSymbols { get; }
}

internal sealed record RepositoryProjectDefinition
{
    public RepositoryProjectDefinition(
        string id,
        string relativePath,
        string targetFramework,
        IReadOnlyList<string> directOwnedReferences)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A project ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("A project path is required.", nameof(relativePath));
        if (string.IsNullOrWhiteSpace(targetFramework))
            throw new ArgumentException("A target framework is required.", nameof(targetFramework));
        ArgumentNullException.ThrowIfNull(directOwnedReferences);

        Id = id;
        RelativePath = relativePath;
        TargetFramework = targetFramework;
        DirectOwnedReferences = new ReadOnlyCollection<string>(directOwnedReferences.ToArray());
    }

    public string Id { get; }

    public string RelativePath { get; }

    public string TargetFramework { get; }

    public IReadOnlyList<string> DirectOwnedReferences { get; }
}

internal sealed record RepositoryGraphNodeKey
{
    internal RepositoryGraphNodeKey(string projectId, string targetFramework, string profileId)
    {
        Validate(projectId, nameof(projectId));
        Validate(targetFramework, nameof(targetFramework));
        Validate(profileId, nameof(profileId));
        ProjectId = projectId;
        TargetFramework = targetFramework;
        ProfileId = profileId;
    }

    internal string ProjectId { get; }
    internal string TargetFramework { get; }
    internal string ProfileId { get; }

    private static void Validate(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("Node identity segments must be nonempty and contain no path separators.", name);
    }
}

internal sealed record RepositoryGraphNode
{
    public RepositoryGraphNode(string projectId, string projectPath, string targetFramework, BuildProfile profile)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("A project ID is required.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("A project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(targetFramework))
            throw new ArgumentException("A target framework is required.", nameof(targetFramework));
        ArgumentNullException.ThrowIfNull(profile);

        ProjectId = projectId;
        ProjectPath = projectPath;
        TargetFramework = targetFramework;
        Profile = profile;
    }

    public string ProjectId { get; }

    public string ProjectPath { get; }

    public string TargetFramework { get; }

    public BuildProfile Profile { get; }

    public RepositoryGraphNodeKey Key => new(ProjectId, TargetFramework, Profile.Id);
}

internal sealed record RepositoryProjectEdge(string FromProjectId, string ToProjectId);

/// <summary>Resolves one target from a multi-target project only when the choice is explicit.</summary>
internal static class RepositoryTargetFrameworkSelection
{
    internal static string Select(IReadOnlyList<string> candidates, string? configuredSelection)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        string[] distinct = candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (distinct.Length == 0)
            throw new InvalidDataException("A project must expose at least one target framework.");
        if (configuredSelection is null)
        {
            if (distinct.Length != 1)
                throw new InvalidDataException("Multi-target project selection is ambiguous without an explicit target framework.");
            return distinct[0];
        }

        if (!distinct.Contains(configuredSelection, StringComparer.Ordinal))
            throw new InvalidDataException($"Configured target framework '{configuredSelection}' is not available.");
        return configuredSelection;
    }
}

/// <summary>Immutable, validated model of the fixed four-project/four-profile graph.</summary>
internal sealed class RepositoryCompilationGraph
{
    private static readonly RepositoryProjectDefinition[] FixedProjects =
    {
        new("machine", "src/SIL.Machine/SIL.Machine.csproj", "netstandard2.0", Array.Empty<string>()),
        new("hc", "src/SIL.Machine.Morphology.HermitCrab/SIL.Machine.Morphology.HermitCrab.csproj", "netstandard2.0", new[] { "machine" }),
        new("hc-tool", "src/SIL.Machine.Morphology.HermitCrab.Tool/SIL.Machine.Morphology.HermitCrab.Tool.csproj", "net10.0", new[] { "hc" }),
        new("hc-conformance", "src/SIL.Machine.Morphology.HermitCrab.Conformance/SIL.Machine.Morphology.HermitCrab.Conformance.csproj", "net10.0", new[] { "hc", "hc-tool" }),
    };

    private static readonly BuildProfile[] FixedProfiles =
    {
        new("base", Array.Empty<string>()),
        new("single-threaded", new[] { "SINGLE_THREADED" }),
        new("output-analyses", new[] { "OUTPUT_ANALYSES" }),
        new("combined", new[] { "SINGLE_THREADED", "OUTPUT_ANALYSES" }),
    };

    private RepositoryCompilationGraph(
        IReadOnlyList<RepositoryProjectDefinition> projects,
        IReadOnlyList<BuildProfile> profiles,
        IReadOnlyList<RepositoryGraphNode> nodes,
        IReadOnlyList<RepositoryProjectEdge> projectEdges,
        IReadOnlyDictionary<RepositoryGraphNodeKey, CapturedCompilerInputs>? captures = null,
        IReadOnlyDictionary<RepositoryGraphNodeKey, CompilerInputModel>? compilerInputs = null,
        CompilationGraphHashInputs? hashInputs = null,
        GraphHashes? hashes = null)
    {
        Projects = new ReadOnlyCollection<RepositoryProjectDefinition>(projects.ToArray());
        Profiles = new ReadOnlyCollection<BuildProfile>(profiles.ToArray());
        Nodes = new ReadOnlyCollection<RepositoryGraphNode>(nodes.ToArray());
        ProjectEdges = new ReadOnlyCollection<RepositoryProjectEdge>(projectEdges.ToArray());
        Captures = new ReadOnlyDictionary<RepositoryGraphNodeKey, CapturedCompilerInputs>(
            new Dictionary<RepositoryGraphNodeKey, CapturedCompilerInputs>(
                captures ?? new Dictionary<RepositoryGraphNodeKey, CapturedCompilerInputs>()));
        CompilerInputs = new ReadOnlyDictionary<RepositoryGraphNodeKey, CompilerInputModel>(
            new Dictionary<RepositoryGraphNodeKey, CompilerInputModel>(
                compilerInputs ?? new Dictionary<RepositoryGraphNodeKey, CompilerInputModel>()));
        _hashInputs = hashInputs;
        _hashes = hashes;
    }

    internal IReadOnlyList<RepositoryProjectDefinition> Projects { get; }

    internal IReadOnlyList<BuildProfile> Profiles { get; }

    internal IReadOnlyList<RepositoryGraphNode> Nodes { get; }

    internal IReadOnlyList<RepositoryProjectEdge> ProjectEdges { get; }

    internal IReadOnlyDictionary<RepositoryGraphNodeKey, CapturedCompilerInputs> Captures { get; }

    internal IReadOnlyDictionary<RepositoryGraphNodeKey, CompilerInputModel> CompilerInputs { get; }

    private readonly CompilationGraphHashInputs? _hashInputs;
    private readonly GraphHashes? _hashes;

    internal CompilationGraphHashInputs HashInputs =>
        _hashInputs ?? throw new InvalidOperationException("Compilation graph hashes have not been attached.");

    internal GraphHashes Hashes =>
        _hashes ?? throw new InvalidOperationException("Compilation graph hashes have not been attached.");

    internal static RepositoryCompilationGraph CreateFixed()
    {
        var nodes = new List<RepositoryGraphNode>(FixedProjects.Length * FixedProfiles.Length);
        foreach (RepositoryProjectDefinition project in FixedProjects)
        {
            foreach (BuildProfile profile in FixedProfiles)
                nodes.Add(new RepositoryGraphNode(project.Id, project.RelativePath, project.TargetFramework, profile));
        }

        return Create(nodes, FixedEdges());
    }

    internal static RepositoryCompilationGraph Create(
        IEnumerable<RepositoryGraphNode> nodes,
        IEnumerable<RepositoryProjectEdge> projectEdges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(projectEdges);
        RepositoryGraphNode[] nodeArray = nodes.ToArray();
        RepositoryProjectEdge[] edgeArray = projectEdges.ToArray();
        ValidateNodes(nodeArray);
        ValidateEdges(edgeArray);
        return new RepositoryCompilationGraph(FixedProjects, FixedProfiles, nodeArray, edgeArray);
    }

    internal RepositoryCompilationGraph WithCaptures(
        IReadOnlyDictionary<RepositoryGraphNodeKey, CapturedCompilerInputs> captures)
    {
        ArgumentNullException.ThrowIfNull(captures);
        if (captures.Count != Nodes.Count || !captures.Keys.ToHashSet().SetEquals(Nodes.Select(node => node.Key)))
            throw new InvalidDataException("Compiler captures must cover every fixed graph node exactly once.");
        return new RepositoryCompilationGraph(Projects, Profiles, Nodes, ProjectEdges, captures, CompilerInputs, _hashInputs, _hashes);
    }

    internal RepositoryCompilationGraph WithCompilerInputs(
        IReadOnlyDictionary<RepositoryGraphNodeKey, CompilerInputModel> compilerInputs)
    {
        ArgumentNullException.ThrowIfNull(compilerInputs);
        if (compilerInputs.Count != Nodes.Count || !compilerInputs.Keys.ToHashSet().SetEquals(Nodes.Select(node => node.Key)))
            throw new InvalidDataException("Normalized compiler inputs must cover every fixed graph node exactly once.");
        return new RepositoryCompilationGraph(Projects, Profiles, Nodes, ProjectEdges, Captures, compilerInputs, _hashInputs, _hashes);
    }

    internal RepositoryCompilationGraph WithHashInputs(CompilationGraphHashInputs hashInputs)
    {
        ArgumentNullException.ThrowIfNull(hashInputs);
        if (Captures.Count != Nodes.Count || CompilerInputs.Count != Nodes.Count)
            throw new InvalidDataException("Hash inputs require complete compiler captures and normalized inputs.");
        if (hashInputs.Nodes.Length != Nodes.Count ||
            !hashInputs.Nodes.Select(node => node.Key).ToHashSet().SetEquals(Nodes.Select(node => node.Key)))
        {
            throw new InvalidDataException("Hash inputs must cover every fixed graph node exactly once.");
        }
        return new RepositoryCompilationGraph(
            Projects,
            Profiles,
            Nodes,
            ProjectEdges,
            Captures,
            CompilerInputs,
            hashInputs,
            null);
    }

    internal RepositoryCompilationGraph WithHashes(GraphHashes hashes)
    {
        ArgumentNullException.ThrowIfNull(hashes);
        if (_hashInputs is null)
            throw new InvalidDataException("Graph hashes require hash inputs.");
        GraphHashes expected = CompilationGraphHashing.Compute(_hashInputs);
        if (!expected.Equals(hashes))
            throw new InvalidDataException("Graph hashes do not match the attached hash inputs.");
        return new RepositoryCompilationGraph(
            Projects,
            Profiles,
            Nodes,
            ProjectEdges,
            Captures,
            CompilerInputs,
            _hashInputs,
            hashes);
    }

    private static IEnumerable<RepositoryProjectEdge> FixedEdges()
    {
        yield return new RepositoryProjectEdge("hc", "machine");
        yield return new RepositoryProjectEdge("hc-tool", "hc");
        yield return new RepositoryProjectEdge("hc-conformance", "hc");
        yield return new RepositoryProjectEdge("hc-conformance", "hc-tool");
    }

    private static void ValidateNodes(IReadOnlyList<RepositoryGraphNode> nodes)
    {
        var expected = (
            from project in FixedProjects
            from profile in FixedProfiles
            select new RepositoryGraphNodeKey(project.Id, project.TargetFramework, profile.Id))
            .ToHashSet();
        var actual = new HashSet<RepositoryGraphNodeKey>();
        foreach (RepositoryGraphNode node in nodes)
        {
            if (!actual.Add(node.Key))
            {
                throw new InvalidDataException($"Duplicate compilation graph node '{node.Key}'.");
            }

            RepositoryProjectDefinition? project = FixedProjects.SingleOrDefault(item => item.Id == node.ProjectId);
            if (project is null || !string.Equals(project.RelativePath, node.ProjectPath, StringComparison.Ordinal)
                || !string.Equals(project.TargetFramework, node.TargetFramework, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Compilation graph node '{node.Key}' is outside the fixed project table.");
            }
            if (!FixedProfiles.Any(profile => string.Equals(profile.Id, node.Profile.Id, StringComparison.Ordinal)
                && profile.AdditionalSymbols.SequenceEqual(node.Profile.AdditionalSymbols, StringComparer.Ordinal)))
            {
                throw new InvalidDataException($"Compilation graph node '{node.Key}' uses an unknown profile.");
            }
        }

        if (!actual.SetEquals(expected))
            throw new InvalidDataException("Compilation graph must contain exactly the sixteen fixed project/profile nodes.");
    }

    private static void ValidateEdges(IReadOnlyList<RepositoryProjectEdge> edges)
    {
        if (edges.Count != edges.Distinct().Count())
            throw new InvalidDataException("Compilation graph contains duplicate project edges.");

        HashSet<string> projectIds = FixedProjects.Select(project => project.Id).ToHashSet(StringComparer.Ordinal);
        foreach (RepositoryProjectEdge edge in edges)
        {
            if (!projectIds.Contains(edge.FromProjectId) || !projectIds.Contains(edge.ToProjectId))
                throw new InvalidDataException("Compilation graph contains a project-external edge.");
            if (string.Equals(edge.FromProjectId, edge.ToProjectId, StringComparison.Ordinal))
                throw new InvalidDataException("Compilation graph contains a self-cycle.");
        }

        var expected = FixedEdges().ToHashSet();
        if (!edges.ToHashSet().SetEquals(expected))
            throw new InvalidDataException("Compilation graph edges do not match the fixed direct-reference table.");

        var outgoing = edges.GroupBy(edge => edge.FromProjectId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.ToProjectId).ToArray(), StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (string projectId in projectIds)
            Visit(projectId, outgoing, visiting, visited);
    }

    private static void Visit(
        string projectId,
        IReadOnlyDictionary<string, string[]> outgoing,
        ISet<string> visiting,
        ISet<string> visited)
    {
        if (visited.Contains(projectId))
            return;
        if (!visiting.Add(projectId))
            throw new InvalidDataException("Compilation graph contains a cycle.");
        if (outgoing.TryGetValue(projectId, out string[]? dependencies))
        {
            foreach (string dependency in dependencies)
                Visit(dependency, outgoing, visiting, visited);
        }
        visiting.Remove(projectId);
        visited.Add(projectId);
    }
}
