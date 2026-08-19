#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// How a catalog feature participates in semantic coverage. Only <see cref="Semantic"/> features
/// require fixture evidence; every other disposition must carry a reason and a citation.
/// </summary>
public enum FeatureDisposition
{
    /// <summary>Observable parsing semantics; requires phase effects and fixture evidence.</summary>
    Semantic,

    /// <summary>Load-time structure with no effect on a parse result.</summary>
    Loader,

    /// <summary>Schema bookkeeping such as an attribute's declared type or default.</summary>
    Metadata,

    /// <summary>Deliberately out of scope.</summary>
    Ignored,

    /// <summary>Declared by the schema but rejected or unreachable in practice.</summary>
    Invalid,

    /// <summary>Blocked by a known C# defect; cannot be covered until that is fixed.</summary>
    BlockedCSharpDefect,

    /// <summary>
    /// Mapped by the bootstrap generator and not yet classified by a human. Keeps the exact-once
    /// mapping total while stating plainly that the semantic judgement has not been made, instead of
    /// fabricating phase effects to satisfy the audit.
    /// </summary>
    Unclassified,
}

public sealed record PhaseEffect(string Behavior, IReadOnlyList<string> Reads, IReadOnlyList<string> Writes);

public sealed record SemanticFeature(
    string Id,
    FeatureDisposition Disposition,
    PhaseEffect? AnalysisCandidateEffect,
    PhaseEffect? SynthesisConfirmationEffect,
    PhaseEffect? FinalParseEffect,
    IReadOnlyList<string> Carriers,
    string? Reason,
    IReadOnlyList<string> Citations
);

public sealed record SurfaceMapping(string SurfaceId, string FeatureId);

public sealed record SemanticCatalog(
    string Profile,
    IReadOnlyList<string> AuditedSourceScopes,
    IReadOnlyList<SemanticFeature> Features,
    IReadOnlyList<SurfaceMapping> SurfaceMappings
);

public sealed class SemanticCatalogException(string path, string detail)
    : FormatException($"{path}: {detail}");

public static class SemanticCatalogLoader
{
    public const string ExpectedProfile = "sil.machine.hc-semantic-catalog/v1";

    private static readonly HashSet<string> RootKeys = new(StringComparer.Ordinal)
    {
        "profile",
        "auditedSourceScopes",
        "features",
        "surfaceMappings",
    };
    private static readonly HashSet<string> FeatureKeys = new(StringComparer.Ordinal)
    {
        "id",
        "disposition",
        "analysisCandidateEffect",
        "synthesisConfirmationEffect",
        "finalParseEffect",
        "carriers",
        "reason",
        "citations",
    };
    private static readonly HashSet<string> EffectKeys = new(StringComparer.Ordinal) { "behavior", "reads", "writes" };
    private static readonly HashSet<string> MappingKeys = new(StringComparer.Ordinal) { "surface", "feature" };

    private static readonly Dictionary<string, FeatureDisposition> Dispositions = new(StringComparer.Ordinal)
    {
        ["semantic"] = FeatureDisposition.Semantic,
        ["loader"] = FeatureDisposition.Loader,
        ["metadata"] = FeatureDisposition.Metadata,
        ["ignored"] = FeatureDisposition.Ignored,
        ["invalid"] = FeatureDisposition.Invalid,
        ["blocked-csharp-defect"] = FeatureDisposition.BlockedCSharpDefect,
        ["unclassified"] = FeatureDisposition.Unclassified,
    };

    public static SemanticCatalog Load(string path) => Parse(File.ReadAllText(path), path);

    public static SemanticCatalog Parse(string text, string path)
    {
        ArgumentNullException.ThrowIfNull(text);
        YamlMappingNode root;
        try
        {
            var stream = new YamlStream();
            using var reader = new StringReader(text);
            stream.Load(reader);
            if (stream.Documents.Count != 1)
                throw new SemanticCatalogException(path, "expected exactly one YAML document");
            root = stream.Documents[0].RootNode as YamlMappingNode
                ?? throw new SemanticCatalogException(path, "top-level YAML node must be a mapping");
        }
        catch (YamlException ex)
        {
            throw new SemanticCatalogException(path, $"YAML syntax error at {ex.Start}: {ex.Message}");
        }

        RejectUnknownKeys(root, RootKeys, path, "catalog");
        string profile = RequireScalar(root, "profile", path);
        if (profile != ExpectedProfile)
            throw new SemanticCatalogException(path, $"unsupported catalog profile '{profile}'; expected '{ExpectedProfile}'");

        var features = new List<SemanticFeature>();
        var featureIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (YamlMappingNode node in Sequence(root, "features", path))
        {
            SemanticFeature feature = ParseFeature(node, path);
            if (!featureIds.Add(feature.Id))
                throw new SemanticCatalogException(path, $"duplicate feature id '{feature.Id}'");
            features.Add(feature);
        }

        var mappings = new List<SurfaceMapping>();
        foreach (YamlMappingNode node in Sequence(root, "surfaceMappings", path))
        {
            RejectUnknownKeys(node, MappingKeys, path, "surfaceMappings entry");
            mappings.Add(new SurfaceMapping(RequireScalar(node, "surface", path), RequireScalar(node, "feature", path)));
        }

        return new SemanticCatalog(
            profile,
            Strings(root, "auditedSourceScopes", path),
            new ReadOnlyCollection<SemanticFeature>(features.OrderBy(item => item.Id, StringComparer.Ordinal).ToList()),
            new ReadOnlyCollection<SurfaceMapping>(mappings.OrderBy(item => item.SurfaceId, StringComparer.Ordinal).ToList())
        );
    }

    private static SemanticFeature ParseFeature(YamlMappingNode node, string path)
    {
        RejectUnknownKeys(node, FeatureKeys, path, "features entry");
        string id = RequireScalar(node, "id", path);
        string disposition = RequireScalar(node, "disposition", path);
        if (!Dispositions.TryGetValue(disposition, out FeatureDisposition parsed))
            throw new SemanticCatalogException(path, $"feature '{id}' has unknown disposition '{disposition}'");

        return new SemanticFeature(
            id,
            parsed,
            ParseEffect(node, "analysisCandidateEffect", path, id),
            ParseEffect(node, "synthesisConfirmationEffect", path, id),
            ParseEffect(node, "finalParseEffect", path, id),
            Strings(node, "carriers", path),
            node.Children.TryGetValue(new YamlScalarNode("reason"), out YamlNode? reason) ? Scalar(reason, path) : null,
            Strings(node, "citations", path)
        );
    }

    private static PhaseEffect? ParseEffect(YamlMappingNode node, string key, string path, string featureId)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value))
            return null;
        if (value is not YamlMappingNode mapping)
            throw new SemanticCatalogException(path, $"feature '{featureId}' key '{key}' must be a mapping");
        RejectUnknownKeys(mapping, EffectKeys, path, $"{key} of feature '{featureId}'");
        return new PhaseEffect(RequireScalar(mapping, "behavior", path), Strings(mapping, "reads", path), Strings(mapping, "writes", path));
    }

    private static IEnumerable<YamlMappingNode> Sequence(YamlMappingNode root, string key, string path)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value))
            return Array.Empty<YamlMappingNode>();
        if (value is not YamlSequenceNode sequence)
            throw new SemanticCatalogException(path, $"'{key}' must be a sequence");
        return sequence.Select(item => item as YamlMappingNode
            ?? throw new SemanticCatalogException(path, $"every '{key}' entry must be a mapping"));
    }

    private static IReadOnlyList<string> Strings(YamlMappingNode node, string key, string path)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value))
            return Array.Empty<string>();
        if (value is not YamlSequenceNode sequence)
            throw new SemanticCatalogException(path, $"'{key}' must be a sequence");
        return new ReadOnlyCollection<string>(sequence.Select(item => Scalar(item, path)).ToList());
    }

    private static string RequireScalar(YamlMappingNode node, string key, string path) =>
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value)
            ? Scalar(value, path)
            : throw new SemanticCatalogException(path, $"missing required key '{key}'");

    private static string Scalar(YamlNode node, string path) =>
        node is YamlScalarNode { Value: not null } scalar
            ? scalar.Value
            : throw new SemanticCatalogException(path, "expected a scalar value");

    private static void RejectUnknownKeys(YamlMappingNode node, HashSet<string> allowed, string path, string what)
    {
        foreach (YamlNode key in node.Children.Keys)
        {
            string name = Scalar(key, path);
            if (!allowed.Contains(name))
                throw new SemanticCatalogException(path, $"unknown key '{name}' in {what}");
        }
    }
}
