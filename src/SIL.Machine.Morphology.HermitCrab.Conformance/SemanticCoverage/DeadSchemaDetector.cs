#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Finds DTD element names the engine never mentions. The loader reads the grammar document by
/// element name, so an element with no string-literal occurrence anywhere in the engine source
/// cannot influence a parse: it is schema the implementation does not consume, and no conformance
/// grammar can give it semantic coverage.
/// </summary>
public static class DeadSchemaDetector
{
    public const string EngineSourceRelativePath = "src/SIL.Machine.Morphology.HermitCrab";

    public static IReadOnlyCollection<string> ReadEngineSources(string repositoryRoot)
    {
        string root = Path.Combine(repositoryRoot, EngineSourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
        return Directory
            .GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGenerated(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText)
            .ToArray();
    }

    /// <summary>
    /// Which of <paramref name="elementNames"/> appear as a quoted string in no engine source file.
    /// </summary>
    public static IReadOnlySet<string> FindUnreferenced(string repositoryRoot, IEnumerable<string> elementNames)
    {
        ArgumentNullException.ThrowIfNull(elementNames);
        IReadOnlyCollection<string> sources = ReadEngineSources(repositoryRoot);
        var unreferenced = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string name in elementNames.Distinct(StringComparer.Ordinal))
        {
            // A quoted literal is the loader's way of naming an element. nameof(X) names a TYPE, so
            // counting it would let `case nameof(ComplexFeature)` launder a live element into
            // dead-schema, which permanently excuses it from ever needing a fixture.
            string quoted = $"\"{name}\"";
            string nameOf = $"nameof({name})";
            if (!sources.Any(text => text.Contains(quoted, StringComparison.Ordinal) || text.Contains(nameOf, StringComparison.Ordinal)))
                unreferenced.Add(name);
        }

        return unreferenced;
    }

    /// <summary>The DTD element a grammar-observable surface ID belongs to.</summary>
    public static string? OwningElement(string surfaceId)
    {
        ArgumentNullException.ThrowIfNull(surfaceId);
        if (surfaceId.StartsWith(GrammarFeatureUsage.ElementPrefix, StringComparison.Ordinal))
            return surfaceId[GrammarFeatureUsage.ElementPrefix.Length..];
        if (surfaceId.StartsWith(GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal))
        {
            string rest = surfaceId[GrammarFeatureUsage.EnumPrefix.Length..];
            int slash = rest.IndexOf('/');
            return slash < 0 ? rest : rest[..slash];
        }

        return null;
    }

    private static bool IsGenerated(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
