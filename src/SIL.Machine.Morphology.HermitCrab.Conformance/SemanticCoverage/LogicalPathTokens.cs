#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.Immutable;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record LogicalPathRoots
{
    internal LogicalPathRoots(string repositoryRoot, string sdkRoot, string nugetRoot, string generatedRoot)
        : this(repositoryRoot, sdkRoot, new[] { nugetRoot }, generatedRoot)
    {
    }

    internal LogicalPathRoots(
        string repositoryRoot,
        string sdkRoot,
        IReadOnlyList<string> nugetRoots,
        string generatedRoot)
    {
        RepositoryRoot = RequireAbsolute(repositoryRoot, nameof(repositoryRoot));
        SdkRoot = RequireAbsolute(sdkRoot, nameof(sdkRoot));
        ArgumentNullException.ThrowIfNull(nugetRoots);
        if (nugetRoots.Count == 0)
            throw new ArgumentException("At least one NuGet root is required.", nameof(nugetRoots));
        NuGetRoots = nugetRoots.Select((root, index) => RequireAbsolute(root, $"{nameof(nugetRoots)}[{index}]")).ToImmutableArray();
        GeneratedRoot = RequireAbsolute(generatedRoot, nameof(generatedRoot));

        var roots = new[] { RepositoryRoot, SdkRoot, GeneratedRoot }.Concat(NuGetRoots).ToArray();
        if (roots.Length != roots.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidDataException("Logical path roots must be distinct.");
        for (int i = 0; i < roots.Length; i++)
        {
            for (int j = i + 1; j < roots.Length; j++)
            {
                if (LogicalPathTokens.IsFilesystemRoot(roots[i]) || LogicalPathTokens.IsFilesystemRoot(roots[j]))
                    continue;
                if (LogicalPathTokens.IsUnder(roots[i], roots[j]) || LogicalPathTokens.IsUnder(roots[j], roots[i]))
                    throw new InvalidDataException("Logical path roots must be disjoint and unambiguous.");
            }
        }
    }

    internal string RepositoryRoot { get; }
    internal string SdkRoot { get; }
    internal ImmutableArray<string> NuGetRoots { get; }
    internal string NuGetRoot => NuGetRoots[0];
    internal string GeneratedRoot { get; }

    private static string RequireAbsolute(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path) || !LogicalPathTokens.IsAbsolute(path))
            throw new ArgumentException("A logical path root must be absolute.", parameterName);
        return LogicalPathTokens.NormalizeAbsolute(path);
    }
}

internal static class LogicalPathTokens
{
    private const string AncestorEditorConfigToken = "ancestor-editorconfig";
    private const string EditorConfigFileName = ".editorconfig";

    internal static bool IsLogicalPath(string path) =>
        IsLogicalPathCore(path, out _);

    private static bool IsLogicalPathCore(string path, out string? token)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            token = null;
            return false;
        }
        token = new[] { "repo:/", "sdk:/", "nuget:/", "generated:/", AncestorEditorConfigToken + ":/" }
            .FirstOrDefault(candidate => path.StartsWith(candidate, StringComparison.Ordinal));
        if (token is null || path.Contains('\\'))
            return false;
        string remainder = path[token.Length..];
        return remainder.Length == 0
            || (!remainder.StartsWith("/", StringComparison.Ordinal)
                && !remainder.EndsWith("/", StringComparison.Ordinal)
                && !remainder.Split('/').Any(segment => segment is "" or "." or ".."));
    }

    internal static string FromAbsolute(string absolutePath, LogicalPathRoots roots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);
        ArgumentNullException.ThrowIfNull(roots);

        string normalized = NormalizeAbsolute(absolutePath);
        var candidates = new List<(string Name, string Root, string? PackageId, string? Version)>();
        candidates.Add(("repo", roots.RepositoryRoot, null, null));
        candidates.Add(("sdk", roots.SdkRoot, null, null));
        candidates.Add(("generated", roots.GeneratedRoot, null, null));
        foreach (string nugetRoot in roots.NuGetRoots)
        {
            if (!IsUnder(normalized, nugetRoot))
                continue;
            string relativeToRoot = normalized.Length == nugetRoot.Length
                ? string.Empty
                : normalized[(nugetRoot.Length + 1)..];
            string[] segments = relativeToRoot.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2 || !IsPackageId(segments[0]) || !IsPackageVersion(segments[1]))
                throw new InvalidDataException($"NuGet path '{absolutePath}' is not decomposable into package ID and version.");
            candidates.Add(("nuget", nugetRoot, segments[0], segments[1]));
        }

        (string Name, string Root)[] matches = candidates
            .Where(candidate => candidate.Name != "nuget" && IsUnder(normalized, candidate.Root)
                || candidate.Name == "nuget")
            .OrderByDescending(candidate => candidate.Root.Length)
            .Select(candidate => (candidate.Name, candidate.Root))
            .ToArray();
        if (matches.Length == 0)
            throw new InvalidDataException($"Path '{absolutePath}' is outside all admitted logical roots.");
        (string Name, string Root) match = matches[0];

        bool rootIsSeparator = match.Root.EndsWith("/", StringComparison.Ordinal);
        string relative = normalized.Length == match.Root.Length
            ? string.Empty
            : normalized[(match.Root.Length + (rootIsSeparator ? 0 : 1))..];
        if (match.Name == "nuget")
        {
            string[] segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            relative = string.Join('/', segments);
        }
        return relative.Length == 0
            ? match.Name + ":/"
            : match.Name + ":/" + relative;
    }

    /// <summary>
    /// Resolves like <see cref="FromAbsolute"/>, but also admits an ancestor `.editorconfig`: MSBuild's
    /// EditorConfigFiles item walks up from the project to every ancestor directory, which can land
    /// outside every admitted root when a repository is checked out inside another one (this
    /// repository's own worktree layout does exactly that). Such a file's content still affects
    /// compilation and must be hashed, but admitting it by physical path would break relocation
    /// invariance, so it is identified only by how many directory levels separate it from the
    /// repository root. Everything else that falls outside the admitted roots still fails closed.
    /// </summary>
    internal static string FromAbsoluteAdmittingAncestorEditorConfig(string absolutePath, LogicalPathRoots roots)
    {
        try
        {
            return FromAbsolute(absolutePath, roots);
        }
        catch (InvalidDataException) when (TryAncestorEditorConfigToken(absolutePath, roots.RepositoryRoot, out string? token))
        {
            return token!;
        }
    }

    private static bool TryAncestorEditorConfigToken(string absolutePath, string repositoryRoot, out string? logicalPath)
    {
        logicalPath = null;
        if (!string.Equals(Path.GetFileName(absolutePath), EditorConfigFileName, StringComparison.OrdinalIgnoreCase))
            return false;

        string[] fileSegments = NormalizeAbsolute(absolutePath).Split('/');
        if (fileSegments.Length < 2)
            return false;
        string[] parentSegments = fileSegments[..^1];
        string[] repositorySegments = repositoryRoot.Split('/');
        if (parentSegments.Length >= repositorySegments.Length
            || !parentSegments.AsSpan().SequenceEqual(
                repositorySegments.AsSpan(0, parentSegments.Length),
                StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        logicalPath = $"{AncestorEditorConfigToken}:/{repositorySegments.Length - parentSegments.Length}";
        return true;
    }

    private static bool IsPackageId(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.IndexOfAny(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) < 0;

    private static bool IsPackageVersion(string value) =>
        IsPackageId(value) && char.IsDigit(value[0]) && value.Any(char.IsLetterOrDigit);

    internal static void ValidateUnique(IEnumerable<string> logicalPaths)
    {
        ArgumentNullException.ThrowIfNull(logicalPaths);
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string? path in logicalPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidDataException($"Logical paths contain a case-insensitive collision at '{path}'.");
            if (seen.ContainsKey(path))
            {
                throw new InvalidDataException($"Logical paths contain a case-insensitive collision at '{path}'.");
            }
            seen[path] = path;
        }
    }

    internal static string NormalizeAbsolute(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!IsAbsolute(path))
            throw new InvalidDataException($"Path '{path}' is not absolute.");
        if (HasTraversalSegment(path))
            throw new InvalidDataException($"Path '{path}' contains a traversal segment.");

        string normalized = path.Replace('\\', '/');
        bool isDriveRoot = normalized.Length >= 3 && normalized[1] == ':' && normalized[2] == '/';
        normalized = TrimTrailingSeparators(normalized, isDriveRoot);
        return normalized;
    }

    internal static bool IsAbsolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        return path.StartsWith("/", StringComparison.Ordinal)
            || (path.Length >= 3
                && char.IsLetter(path[0])
                && path[1] == ':'
                && (path[2] == '/' || path[2] == '\\'))
            || path.StartsWith("//", StringComparison.Ordinal)
            || path.StartsWith("\\\\", StringComparison.Ordinal);
    }

    internal static bool IsUnder(string path, string root) =>
        path.Equals(root, StringComparison.OrdinalIgnoreCase)
        || (path.Length > root.Length
            && path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            && (root.EndsWith("/", StringComparison.Ordinal) || path[root.Length] == '/'));

    internal static bool IsFilesystemRoot(string path) =>
        path == "/" || (path.Length == 3 && path[1] == ':' && path[2] == '/');

    private static bool HasTraversalSegment(string path) =>
        path.Replace('\\', '/').Split('/').Any(segment => segment is "." or "..");

    private static string TrimTrailingSeparators(string path, bool isDriveRoot)
    {
        int length = path.Length;
        while (length > 0 && path[length - 1] == '/')
            length--;
        if (length == 0)
            return "/";
        if (isDriveRoot && length == 2)
            return path[..3];
        return path[..length];
    }
}
