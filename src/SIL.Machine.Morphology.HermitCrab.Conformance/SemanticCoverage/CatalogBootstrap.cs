#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Generates a catalog that maps every generated surface exactly once, so the audit runs against the
/// real inventory instead of only against literals in tests. Grammar-observable surfaces are grouped
/// per DTD element and left <c>unclassified</c>: the bootstrap states plainly that no human has judged
/// them rather than inventing phase effects to satisfy the audit. Everything else is classified by
/// kind, which is where the surfaces a grammar author cannot write are accounted for by name.
/// </summary>
public static class CatalogBootstrap
{
    public const string CatalogRelativePath = "conformance/semantic-catalog.yaml";

    private sealed record KindPolicy(string Disposition, string Reason);

    private static readonly Dictionary<string, KindPolicy> KindPolicies = new(StringComparer.Ordinal)
    {
        ["attribute"] = new(
            "loader",
            "An attribute declaration itself; its enumerated values are the unit a grammar can choose, and those are classified separately."
        ),
        ["attribute-type"] = new(
            "metadata",
            "The declared XML type of an attribute. Schema bookkeeping with no parse behaviour of its own."
        ),
        ["attribute-default"] = new(
            "metadata",
            "The declared default of an attribute. Which value is in effect is observable; the declaration of the default is not."
        ),
        ["default"] = new("metadata", "A DTD default declaration. Schema bookkeeping."),
        ["content-group"] = new(
            "loader",
            "A content-model group. Constrains what a document may contain; the loader enforces it before any parse behaviour exists."
        ),
        ["placement"] = new(
            "loader",
            "Child occurrence and cardinality within a content model. Genuinely semantic as occurrence multiplicity, but not writable as a value in a grammar, so it is out of reach of the coverage ledger."
        ),
        ["special-content"] = new("metadata", "An EMPTY or ANY content declaration. Schema bookkeeping."),
        ["xml-read"] = new(
            "loader",
            "A loader read of a named element or attribute. Load-time structure, tracked by the C# census rather than by grammar coverage."
        ),
        ["xml-all-elements"] = new("loader", "A loader read of every child element. Load-time structure."),
        ["dynamic-xml-access"] = new("loader", "A loader read whose name is computed. Load-time structure."),
        ["callable"] = new("loader", "A loader method. Load-time structure."),
        ["enum-member"] = new("loader", "A C# enum member of the loader model."),
        ["rule-implementation"] = new("loader", "A concrete rule type in the engine model."),
        ["decision-if"] = new(
            "unclassified",
            "A conditional branch in an audited C# scope; semantic phase effects require human classification."
        ),
        ["decision-switch"] = new(
            "unclassified",
            "A switch branch in an audited C# scope; semantic phase effects require human classification."
        ),
        ["decision-switch-expression"] = new(
            "unclassified",
            "A switch-expression arm in an audited C# scope; semantic phase effects require human classification."
        ),
        ["decision-conditional"] = new(
            "unclassified",
            "A conditional expression branch in an audited C# scope; semantic phase effects require human classification."
        ),
        ["decision-conditional-access"] = new(
            "unclassified",
            "A null-conditional access branch in an audited C# scope; semantic phase effects require human classification."
        ),
        ["decision-coalesce"] = new(
            "unclassified",
            "A null-coalescing branch in an audited C# scope; semantic phase effects require human classification."
        ),
        ["decision-catch"] = new(
            "unclassified",
            "A catch branch in an audited C# scope; semantic phase effects require human classification."
        ),
        ["decision-catch-filter"] = new(
            "unclassified",
            "A catch-filter branch in an audited C# scope; semantic phase effects require human classification."
        ),
        ["decision-loop"] = new(
            "unclassified",
            "A loop branch in an audited C# scope; semantic phase effects require human classification."
        ),
    };

    public static string Generate(SemanticInventory inventory) => Generate(inventory, Array.Empty<string>());

    public static string Generate(SemanticInventory inventory, IReadOnlyCollection<string> auditedSourceScopes)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        string[] scopes = NormalizeScopes(auditedSourceScopes);
        var observable = GrammarFeatureUsage.Observable(inventory).ToHashSet(StringComparer.Ordinal);
        var mappings = new List<(string SurfaceId, string FeatureId)>();
        var elementFeatures = new SortedSet<string>(StringComparer.Ordinal);
        var kindFeatures = new SortedSet<string>(StringComparer.Ordinal);
        var unknownKinds = new SortedSet<string>(StringComparer.Ordinal);

        foreach (InventorySurface surface in inventory.Surfaces.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (observable.Contains(surface.Id) && DeadSchemaDetector.OwningElement(surface.Id) is string element)
            {
                string featureId = $"grammar/{element}";
                elementFeatures.Add(element);
                mappings.Add((surface.Id, featureId));
                continue;
            }

            if (!KindPolicies.ContainsKey(surface.Kind))
                unknownKinds.Add(surface.Kind);
            string kindFeature = $"schema/{surface.Kind}";
            kindFeatures.Add(surface.Kind);
            mappings.Add((surface.Id, kindFeature));
        }

        if (unknownKinds.Count != 0)
        {
            throw new InvalidOperationException(
                $"No catalog policy for surface kind(s): {string.Join(", ", unknownKinds)}. "
                    + "Add a policy rather than letting a new kind fall through unclassified."
            );
        }

        var text = new StringBuilder();
        text.Append(Header(inventory, elementFeatures.Count, kindFeatures.Count, mappings.Count));
        text.Append("profile: ").Append(SemanticCatalogLoader.ExpectedProfile).Append('\n');
        text.Append("auditedSourceScopes: [").Append(string.Join(", ", scopes.Select(QuoteYamlScalar))).Append("]\n");
        text.Append("features:\n");
        foreach (string element in elementFeatures)
        {
            text.Append($"  - id: grammar/{element}\n");
            text.Append("    disposition: unclassified\n");
            text.Append(
                $"    reason: \"Grammar-observable surfaces of DTD element {element}. Generated by the bootstrap; no human has judged the phase effects yet. Per-surface coverage is tracked in the coverage ledger.\"\n"
            );
            text.Append(
                "    citations: [\"src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd\", \"conformance/semantic-coverage-baseline.txt\"]\n"
            );
        }

        foreach (string kind in kindFeatures)
        {
            KindPolicy policy = KindPolicies[kind];
            text.Append($"  - id: schema/{kind}\n");
            text.Append($"    disposition: {policy.Disposition}\n");
            text.Append($"    reason: \"{policy.Reason}\"\n");
            text.Append("    citations: [\"src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd\"]\n");
        }

        text.Append("surfaceMappings:\n");
        foreach (
            (string surfaceId, string featureId) in mappings.OrderBy(item => item.SurfaceId, StringComparer.Ordinal)
        )
        {
            text.Append($"  - surface: \"{surfaceId}\"\n");
            text.Append($"    feature: {featureId}\n");
        }

        return text.ToString();
    }

    /// <summary>
    /// Writes a bootstrap proposal to the supplied stream. This is deliberately the only write
    /// surface: proposals are review material and can never overwrite the curated catalog.
    /// </summary>
    public static void WriteProposal(TextWriter writer, SemanticInventory inventory) =>
        WriteProposal(writer, inventory, Array.Empty<string>());

    public static void WriteProposal(
        TextWriter writer,
        SemanticInventory inventory,
        IReadOnlyCollection<string> auditedSourceScopes
    )
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(inventory);
        writer.Write(Generate(inventory, auditedSourceScopes));
    }

    public static SemanticCatalog Load(string repositoryRoot) =>
        SemanticCatalogLoader.Load(
            Path.Combine(repositoryRoot, CatalogRelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

    private static string[] NormalizeScopes(IReadOnlyCollection<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        foreach (string? scope in scopes)
        {
            if (string.IsNullOrWhiteSpace(scope) || ScopeValidation.HasPattern(scope))
            {
                throw new ArgumentException(
                    $"Audited source scope '{scope}' must be one exact canonical symbol ID.",
                    nameof(scopes)
                );
            }
        }

        return scopes.Distinct(StringComparer.Ordinal).OrderBy(scope => scope, StringComparer.Ordinal).ToArray();
    }

    private static string QuoteYamlScalar(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)}\"";

    private static string Header(SemanticInventory inventory, int elementFeatures, int kindFeatures, int mappings) =>
        $"""
            # HermitCrab semantic catalog
            #
            # PROPOSAL from hc-conformance --propose-semantic-catalog. Every one of the {inventory.Surfaces.Count} surfaces the
            # inventory generates is named by exactly one surfaceMappings row, so a surface added to the DTD
            # is unmapped and the audit fails until someone classifies it. That fail-closed property is the
            # whole point; regenerating is a deliberate act, not something a build does for you.
            #
            # {elementFeatures} features group the grammar-observable surfaces by DTD element and are
            # `unclassified`: the bootstrap records that no human has judged their phase effects, rather
            # than fabricating effects to satisfy the audit. Promoting one to `semantic` means supplying all
            # three phase effects, which the audit then requires.
            #
            # {kindFeatures} features classify the rest by surface kind. This is where the surfaces a grammar
            # author cannot write are accounted for by name instead of vanishing into a filter.
            #
            # {mappings} mapping rows. Wildcards are rejected, not expanded.

            """;
}
