#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

public sealed record AuditDiagnostic(
    string Code,
    string SubjectId,
    string Message,
    string Configurations = "",
    string Location = "");

public sealed record AuditResult(bool IsComplete, IReadOnlyList<AuditDiagnostic> Diagnostics);

/// <summary>
/// Joins a generated <see cref="SemanticInventory"/> to a curated <see cref="SemanticCatalog"/> and
/// reports every way the join is incomplete. Many surfaces may share one feature, but each surface
/// must be named by exactly one mapping row, so a newly generated surface stays red until a human
/// classifies it.
/// </summary>
public static class SemanticCoverageAudit
{
    public const string UnmappedSurface = "unmapped-surface";
    public const string DuplicateSurfaceMapping = "duplicate-surface-mapping";
    public const string StaleSurfaceMapping = "stale-surface-mapping";
    public const string UnknownFeature = "unknown-feature";
    public const string PatternMapping = "pattern-mapping";
    public const string UnusedFeature = "unused-feature";
    public const string MissingPhaseEffect = "missing-phase-effect";
    public const string RetirementWithoutReason = "retirement-without-reason";
    public const string UnclassifiedMapping = "unclassified-mapping";

    private static readonly char[] PatternCharacters = { '*', '?', '[', ']' };

    public static AuditResult Run(SemanticInventory inventory, SemanticCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(catalog);

        var diagnostics = new List<AuditDiagnostic>();
        foreach (InventoryDiagnostic diagnostic in inventory.Diagnostics)
        {
            diagnostics.Add(new AuditDiagnostic(
                diagnostic.Code, diagnostic.SubjectId, diagnostic.Message,
                diagnostic.Configurations, diagnostic.Location));
        }
        var featuresById = new Dictionary<string, SemanticFeature>(StringComparer.Ordinal);
        foreach (SemanticFeature feature in catalog.Features)
            featuresById[feature.Id] = feature;

        foreach (SemanticFeature feature in catalog.Features)
        {
            if (feature.Disposition == FeatureDisposition.Semantic)
            {
                foreach ((string name, PhaseEffect? effect) in Effects(feature))
                {
                    if (effect is null)
                    {
                        diagnostics.Add(new AuditDiagnostic(
                            MissingPhaseEffect, feature.Id,
                            $"semantic feature '{feature.Id}' declares no {name}"));
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(feature.Reason) || feature.Citations.Count == 0)
            {
                diagnostics.Add(new AuditDiagnostic(
                    RetirementWithoutReason, feature.Id,
                    $"feature '{feature.Id}' is {Name(feature.Disposition)} and needs a reason and at least one citation"));
            }
        }

        var mappedSurfaces = new HashSet<string>(StringComparer.Ordinal);
        var usedFeatures = new HashSet<string>(StringComparer.Ordinal);
        var inventoryIds = new HashSet<string>(inventory.Surfaces.Select(surface => surface.Id), StringComparer.Ordinal);
        foreach (SurfaceMapping mapping in catalog.SurfaceMappings)
        {
            if (mapping.SurfaceId.IndexOfAny(PatternCharacters) >= 0)
            {
                diagnostics.Add(new AuditDiagnostic(
                    PatternMapping, mapping.SurfaceId,
                    $"mapping '{mapping.SurfaceId}' must name one exact surface; patterns are not allowed"));
                continue;
            }

            if (!mappedSurfaces.Add(mapping.SurfaceId))
            {
                diagnostics.Add(new AuditDiagnostic(
                    DuplicateSurfaceMapping, mapping.SurfaceId,
                    $"surface '{mapping.SurfaceId}' is mapped more than once"));
            }

            if (!featuresById.ContainsKey(mapping.FeatureId))
            {
                diagnostics.Add(new AuditDiagnostic(
                    UnknownFeature, mapping.SurfaceId,
                    $"surface '{mapping.SurfaceId}' maps to undeclared feature '{mapping.FeatureId}'"));
            }
            else
            {
                usedFeatures.Add(mapping.FeatureId);
                if (featuresById[mapping.FeatureId].Disposition == FeatureDisposition.Unclassified)
                {
                    diagnostics.Add(new AuditDiagnostic(
                        UnclassifiedMapping,
                        mapping.SurfaceId,
                        $"surface '{mapping.SurfaceId}' maps to unclassified feature '{mapping.FeatureId}'"));
                }
            }

            if (!inventoryIds.Contains(mapping.SurfaceId))
            {
                diagnostics.Add(new AuditDiagnostic(
                    StaleSurfaceMapping, mapping.SurfaceId,
                    $"mapping names '{mapping.SurfaceId}', which the generated inventory no longer contains"));
            }
        }

        foreach (InventorySurface surface in inventory.Surfaces)
        {
            if (!mappedSurfaces.Contains(surface.Id))
            {
                diagnostics.Add(new AuditDiagnostic(
                    UnmappedSurface, surface.Id,
                    $"generated surface '{surface.Id}' ({surface.Kind}) has no catalog mapping"));
            }
        }

        foreach (SemanticFeature feature in catalog.Features)
        {
            if (!usedFeatures.Contains(feature.Id))
            {
                diagnostics.Add(new AuditDiagnostic(
                    UnusedFeature, feature.Id,
                    $"feature '{feature.Id}' is declared but no surface maps to it"));
            }
        }

        IReadOnlyList<AuditDiagnostic> ordered = new ReadOnlyCollection<AuditDiagnostic>(
            diagnostics
                .OrderBy(item => item.Code, StringComparer.Ordinal)
                .ThenBy(item => item.SubjectId, StringComparer.Ordinal)
                .ThenBy(item => item.Location, StringComparer.Ordinal)
                .ThenBy(item => item.Configurations, StringComparer.Ordinal)
                .ThenBy(item => item.Message, StringComparer.Ordinal)
                .ToList()
        );
        return new AuditResult(ordered.Count == 0, ordered);
    }

    private static IEnumerable<(string Name, PhaseEffect? Effect)> Effects(SemanticFeature feature)
    {
        yield return ("analysisCandidateEffect", feature.AnalysisCandidateEffect);
        yield return ("synthesisConfirmationEffect", feature.SynthesisConfirmationEffect);
        yield return ("finalParseEffect", feature.FinalParseEffect);
    }

    private static string Name(FeatureDisposition disposition) =>
        disposition switch
        {
            FeatureDisposition.Loader => "loader",
            FeatureDisposition.Metadata => "metadata",
            FeatureDisposition.Ignored => "ignored",
            FeatureDisposition.Invalid => "invalid",
            FeatureDisposition.BlockedCSharpDefect => "blocked-csharp-defect",
            _ => "semantic",
        };
}
