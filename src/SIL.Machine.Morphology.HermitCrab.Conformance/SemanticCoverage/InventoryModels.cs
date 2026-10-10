#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

public sealed record InventorySurface(
    string Id,
    string Kind,
    string Name,
    string? Parent,
    string Source,
    string? Value = null,
    // Comma-joined sorted configuration names, empty when the surface is not
    // configuration-scoped. A string keeps record equality by value.
    string Configurations = ""
);

public sealed record InventoryDiagnostic(string Code, string SubjectId, string Message, string Configurations = "")
{
    public InventoryDiagnostic(
        string code,
        string subjectId,
        string message,
        IReadOnlyCollection<string>? configurations
    )
        : this(code, subjectId, message, JoinConfigurations(configurations)) { }

    public InventoryDiagnostic(
        string code,
        string subjectId,
        string message,
        IReadOnlyCollection<string>? configurations,
        string location
    )
        : this(code, subjectId, message, JoinConfigurations(configurations))
    {
        Location = location;
    }

    public InventoryDiagnostic(string code, string subjectId, string message, string configurations, string location)
        : this(code, subjectId, message, configurations)
    {
        Location = location;
    }

    public string Location { get; init; } = "";

    private static string JoinConfigurations(IReadOnlyCollection<string>? configurations) =>
        configurations is null
            ? string.Empty
            : string.Join(
                ",",
                configurations
                    .Where(value => !string.IsNullOrEmpty(value))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
            );
}

public sealed record SemanticInventory
{
    public SemanticInventory(
        string profile,
        string sourceHash,
        IReadOnlyList<InventorySurface> surfaces,
        IReadOnlyList<InventoryDiagnostic>? diagnostics = null
    )
    {
        Profile = profile;
        SourceHash = sourceHash;
        Surfaces = surfaces;
        Diagnostics = new ReadOnlyCollection<InventoryDiagnostic>(
            (diagnostics ?? Array.Empty<InventoryDiagnostic>()).ToList()
        );
    }

    public string Profile { get; init; }

    public string SourceHash { get; init; }

    public IReadOnlyList<InventorySurface> Surfaces { get; init; }

    public IReadOnlyList<InventoryDiagnostic> Diagnostics { get; init; }
}

public sealed record CSharpInventoryInput
{
    public CSharpInventoryInput(string relativePath, string sourceText, IReadOnlyList<string>? auditedScopes = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(relativePath);
        ArgumentNullException.ThrowIfNull(sourceText);
        ValidateRelativePath(relativePath);
        RelativePath = relativePath.Replace('\\', '/');
        SourceText = sourceText;
        var scopes = auditedScopes ?? Array.Empty<string>();
        if (scopes.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException("Audited source scopes cannot be null or empty.", nameof(auditedScopes));
        }

        AuditedScopes = new ReadOnlyCollection<string>(
            scopes.Distinct(StringComparer.Ordinal).OrderBy(scope => scope, StringComparer.Ordinal).ToList()
        );
    }

    public string RelativePath { get; }

    public string SourceText { get; }

    public IReadOnlyList<string> AuditedScopes { get; }

    private static void ValidateRelativePath(string path)
    {
        if (
            Uri.TryCreate(path, UriKind.Absolute, out _)
            || path.StartsWith("/", StringComparison.Ordinal)
            || path.StartsWith("\\", StringComparison.Ordinal)
            || (path.Length >= 2 && path[1] == ':')
        )
        {
            throw new ArgumentException("C# source paths must be canonical relative paths.", nameof(path));
        }

        string[] segments = path.Replace('\\', '/').Split('/');
        if (segments.Any(segment => segment is "." or ".." or ""))
        {
            throw new ArgumentException("C# source paths cannot contain '.', '..', or empty segments.", nameof(path));
        }
    }
}

/// <param name="CompleteProjects">Assembly names whose sources are present in full. Only such a
/// project may be dropped from the census reference set; a partial source set still needs its own
/// built assembly to resolve the files it does not carry.</param>
public sealed record SemanticCoverageSourceSet(
    string DtdPath,
    string DtdText,
    IReadOnlyList<CSharpInventoryInput> CSharpSources,
    string ToolchainFingerprint = "",
    IReadOnlyList<string>? CompleteProjects = null
)
{
    public static SemanticCoverageSourceSet FromDtd(string path, string text) =>
        new(path, text, Array.Empty<CSharpInventoryInput>());
}

public sealed class SemanticCoverageParseException(string sourcePath, int line, int column, string detail)
    : FormatException($"{sourcePath}:{line}:{column}: {detail}")
{
    public string SourcePath { get; } = sourcePath;

    public int Line { get; } = line;

    public int Column { get; } = column;
}

internal static class CanonicalIdCodec
{
    public static string Encode(string authoredValue)
    {
        ArgumentNullException.ThrowIfNull(authoredValue);
        var builder = new StringBuilder();
        foreach (byte value in Encoding.UTF8.GetBytes(authoredValue))
        {
            if (
                (value >= 'A' && value <= 'Z')
                || (value >= 'a' && value <= 'z')
                || (value >= '0' && value <= '9')
                || value is (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~'
            )
            {
                builder.Append((char)value);
            }
            else
            {
                builder.Append('%').Append(value.ToString("X2"));
            }
        }

        return builder.ToString();
    }
}

internal static class InventorySurfaceFactory
{
    public static IReadOnlyList<InventorySurface> Sort(IEnumerable<InventorySurface> surfaces)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<InventorySurface>();
        foreach (InventorySurface surface in surfaces)
        {
            if (!ids.Add(surface.Id))
            {
                throw new InvalidOperationException($"Duplicate generated surface ID: {surface.Id}");
            }

            ordered.Add(surface);
        }

        return new ReadOnlyCollection<InventorySurface>(
            ordered.OrderBy(surface => surface.Id, StringComparer.Ordinal).ToList()
        );
    }
}
