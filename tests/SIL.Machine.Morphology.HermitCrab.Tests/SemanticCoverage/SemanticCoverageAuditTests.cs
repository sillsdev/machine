using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class SemanticCoverageAuditTests
{
    private const string Dtd = "<!ELEMENT Root EMPTY>";
    private const string Path = "semantic-catalog.yaml";

    private static SemanticInventory Inventory() =>
        SemanticCoverageInventory.Generate(SemanticCoverageSourceSet.FromDtd("fixture.dtd", Dtd));

    private static string Catalog(string features, string mappings) =>
        $"""
        profile: sil.machine.hc-semantic-catalog/v1
        auditedSourceScopes: [Fixture.Root]
        features:
        {features}
        surfaceMappings:
        {mappings}
        """;

    private static string SemanticFeatureYaml(string id) =>
        $"""
          - id: {id}
            disposition: semantic
            analysisCandidateEffect:
              behavior: proposes
              reads: [shape]
              writes: [candidate]
            synthesisConfirmationEffect:
              behavior: confirms
              reads: [candidate]
              writes: [word]
            finalParseEffect:
              behavior: reports
              reads: [word]
              writes: [analysis]
            carriers: [element]
        """;

    private static string MetadataFeatureYaml(string id, string reason, string citations) =>
        $"""
          - id: {id}
            disposition: metadata
            reason: {reason}
            citations: {citations}
        """;

    private static string MappingsFor(SemanticInventory inventory, string featureId) =>
        string.Join(
            "\n",
            inventory.Surfaces.Select(surface => $"  - surface: \"{surface.Id}\"\n    feature: {featureId}")
        );

    [Test]
    public void FullyMappedCatalogPasses()
    {
        SemanticInventory inventory = Inventory();
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("root-element"), MappingsFor(inventory, "root-element")),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.IsComplete, Is.True);
    }

    // The semantic coverage program now audits the grammar format (dtd: surfaces), not the C#
    // engine's internals, so an empty auditedSourceScopes list names no gap and must not fail
    // the audit on its own.
    [Test]
    public void EmptyAuditedSourceScopesDoNotFailTheAudit()
    {
        SemanticInventory inventory = Inventory();
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("root-element"), MappingsFor(inventory, "root-element"))
                .Replace("auditedSourceScopes: [Fixture.Root]", "auditedSourceScopes: []", StringComparison.Ordinal),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.IsComplete, Is.True);
    }

    [Test]
    public void EveryMappingToAnUnclassifiedFeatureIsIncomplete()
    {
        SemanticInventory inventory = Inventory();
        const string Unclassified = """
              - id: pending
                disposition: unclassified
                reason: "not reviewed yet"
                citations: ["HermitCrabInput.dtd"]
            """;
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(Unclassified, MappingsFor(inventory, "pending")),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(result.IsComplete, Is.False);
        Assert.That(
            result.Diagnostics
                .Where(item => item.Code == SemanticCoverageAudit.UnclassifiedMapping)
                .Select(item => item.SubjectId),
            Is.EqualTo(inventory.Surfaces.Select(surface => surface.Id).OrderBy(id => id, StringComparer.Ordinal))
        );
    }

    [Test]
    public void InventoryExecutionDiagnosticsAreAlwaysIncomplete()
    {
        SemanticInventory basis = Inventory();
        SemanticInventory inventory = basis with
        {
            Diagnostics = new[]
            {
                new InventoryDiagnostic(
                    "unresolved-delegate-dispatch",
                    "Fixture.Root.Run(System.Boolean)",
                    "mutable delegate target cannot be closed statically",
                    "base,SINGLE_THREADED",
                    "fixture.cs:4:9-4:24")
            }
        };
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("root-element"), MappingsFor(inventory, "root-element")),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(result.IsComplete, Is.False);
        AuditDiagnostic diagnostic = result.Diagnostics.Single(item => item.Code == "unresolved-delegate-dispatch");
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.SubjectId, Is.EqualTo("Fixture.Root.Run(System.Boolean)"));
            Assert.That(diagnostic.Configurations, Is.EqualTo("base,SINGLE_THREADED"));
            Assert.That(diagnostic.Location, Is.EqualTo("fixture.cs:4:9-4:24"));
        });
    }

    [Test]
    public void UnmappedSurfaceIsReported()
    {
        SemanticInventory inventory = Inventory();
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("root-element"), $"  - surface: \"{inventory.Surfaces[0].Id}\"\n    feature: root-element"),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(result.IsComplete, Is.False);
        Assert.That(
            result.Diagnostics.Select(item => item.Code),
            Does.Contain(SemanticCoverageAudit.UnmappedSurface)
        );
    }

    [Test]
    public void DuplicateMappingIsReported()
    {
        SemanticInventory inventory = Inventory();
        string duplicated = $"  - surface: \"{inventory.Surfaces[0].Id}\"\n    feature: root-element";
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("root-element"), $"{MappingsFor(inventory, "root-element")}\n{duplicated}"),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(
            result.Diagnostics.Where(item => item.Code == SemanticCoverageAudit.DuplicateSurfaceMapping)
                .Select(item => item.SubjectId),
            Is.EqualTo(new[] { inventory.Surfaces[0].Id })
        );
    }

    [Test]
    public void UnknownFeatureIsReported()
    {
        SemanticInventory inventory = Inventory();
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("root-element"), MappingsFor(inventory, "not-declared")),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(
            result.Diagnostics.Select(item => item.Code),
            Does.Contain(SemanticCoverageAudit.UnknownFeature)
        );
    }

    [Test]
    public void PatternMappingIsRejectedRatherThanExpanded()
    {
        SemanticInventory inventory = Inventory();
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("root-element"), "  - surface: \"dtd:element/*\"\n    feature: root-element"),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(
            result.Diagnostics.Where(item => item.Code == SemanticCoverageAudit.PatternMapping)
                .Select(item => item.SubjectId),
            Is.EqualTo(new[] { "dtd:element/*" })
        );
        Assert.That(
            result.Diagnostics.Select(item => item.Code),
            Does.Contain(SemanticCoverageAudit.UnmappedSurface),
            "a pattern must not silently cover the surfaces it resembles"
        );
    }

    [Test]
    public void StaleMappingIsReported()
    {
        SemanticInventory inventory = Inventory();
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(
                SemanticFeatureYaml("root-element"),
                $"{MappingsFor(inventory, "root-element")}\n  - surface: \"dtd:element/Retired\"\n    feature: root-element"
            ),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(
            result.Diagnostics.Where(item => item.Code == SemanticCoverageAudit.StaleSurfaceMapping)
                .Select(item => item.SubjectId),
            Is.EqualTo(new[] { "dtd:element/Retired" })
        );
    }

    [Test]
    public void SemanticFeatureMissingAPhaseEffectIsReported()
    {
        SemanticInventory inventory = Inventory();
        const string Partial = """
              - id: root-element
                disposition: semantic
                analysisCandidateEffect:
                  behavior: proposes
                  reads: [shape]
                  writes: [candidate]
                carriers: [element]
            """;
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(Partial, MappingsFor(inventory, "root-element")),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(
            result.Diagnostics.Where(item => item.Code == SemanticCoverageAudit.MissingPhaseEffect)
                .Select(item => item.Message),
            Has.Exactly(2).Items,
            "the two absent phase effects must each be named"
        );
    }

    [Test]
    public void NonSemanticFeatureWithoutReasonOrCitationIsReported()
    {
        SemanticInventory inventory = Inventory();
        SemanticCatalog missingBoth = SemanticCatalogLoader.Parse(
            Catalog("  - id: schema-noise\n    disposition: metadata", MappingsFor(inventory, "schema-noise")),
            Path
        );
        Assert.That(
            SemanticCoverageAudit.Run(inventory, missingBoth).Diagnostics.Select(item => item.Code),
            Does.Contain(SemanticCoverageAudit.RetirementWithoutReason)
        );

        SemanticCatalog missingCitation = SemanticCatalogLoader.Parse(
            Catalog(MetadataFeatureYaml("schema-noise", "\"declared type only\"", "[]"), MappingsFor(inventory, "schema-noise")),
            Path
        );
        Assert.That(
            SemanticCoverageAudit.Run(inventory, missingCitation).Diagnostics.Select(item => item.Code),
            Does.Contain(SemanticCoverageAudit.RetirementWithoutReason)
        );

        SemanticCatalog complete = SemanticCatalogLoader.Parse(
            Catalog(
                MetadataFeatureYaml("schema-noise", "\"declared type only\"", "[\"HermitCrabInput.dtd\"]"),
                MappingsFor(inventory, "schema-noise")
            ),
            Path
        );
        Assert.That(SemanticCoverageAudit.Run(inventory, complete).Diagnostics, Is.Empty);
    }

    [Test]
    public void DeclaredFeatureWithNoMappingIsReported()
    {
        SemanticInventory inventory = Inventory();
        SemanticCatalog catalog = SemanticCatalogLoader.Parse(
            Catalog(
                $"{SemanticFeatureYaml("root-element")}\n{SemanticFeatureYaml("never-used")}",
                MappingsFor(inventory, "root-element")
            ),
            Path
        );

        AuditResult result = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(
            result.Diagnostics.Where(item => item.Code == SemanticCoverageAudit.UnusedFeature)
                .Select(item => item.SubjectId),
            Is.EqualTo(new[] { "never-used" })
        );
    }

    [Test]
    public void CatalogLoadingIsStrict()
    {
        SemanticInventory inventory = Inventory();
        Assert.Throws<SemanticCatalogException>(() => SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("root-element"), MappingsFor(inventory, "root-element"))
                .Replace("auditedSourceScopes:", "auditedScopes:", StringComparison.Ordinal),
            Path
        ), "unknown root keys are errors");

        Assert.Throws<SemanticCatalogException>(() => SemanticCatalogLoader.Parse(
            Catalog("  - id: x\n    disposition: not-a-disposition", "  - surface: \"a\"\n    feature: x"),
            Path
        ), "unknown dispositions are errors");

        Assert.Throws<SemanticCatalogException>(() => SemanticCatalogLoader.Parse(
            Catalog(SemanticFeatureYaml("dup"), "  - surface: \"a\"\n    feature: dup")
                .Replace("profile: sil.machine.hc-semantic-catalog/v1", "profile: other/v9", StringComparison.Ordinal),
            Path
        ), "an unexpected profile is an error");

        Assert.Throws<SemanticCatalogException>(() => SemanticCatalogLoader.Parse(
            Catalog($"{SemanticFeatureYaml("same")}\n{SemanticFeatureYaml("same")}", "  - surface: \"a\"\n    feature: same"),
            Path
        ), "duplicate feature ids are errors");
    }
}
